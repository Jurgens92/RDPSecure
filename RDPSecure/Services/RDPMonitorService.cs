using System.Net;
using RDPSecure.Services;
using RDPSecure.Logging;
using System.Diagnostics;

namespace RDPSecure.Services
{
    public class RDPMonitorService : IRDPMonitorService
    {
        private readonly AppSettings _settings;
        private readonly Dictionary<string, List<DateTime>> _loginAttempts;
        private readonly Dictionary<string, BanInfo> _bannedIPs;
        private bool _isMonitoring;
        private readonly EventLog _eventLog;
        private readonly ISecurityLogger _logger;

        public event EventHandler<LoginAttemptEventArgs>? LoginAttemptDetected;
        public event EventHandler<IPBanEventArgs>? IPBanned;

        public RDPMonitorService(AppSettings settings)
        {
            try
            {
                _settings = settings;
                _loginAttempts = new Dictionary<string, List<DateTime>>(StringComparer.OrdinalIgnoreCase);
                _bannedIPs = new Dictionary<string, BanInfo>(StringComparer.OrdinalIgnoreCase);
                _logger = new SecurityLogger();

                // Load existing banned IPs
                var savedBans = SettingsManager.LoadBannedIPs();
                foreach (var ban in savedBans)
                {
                    _bannedIPs[ban.Key] = ban.Value;
                    if (DateTime.Now < ban.Value.ExpiryTime)
                    {
                        AddFirewallRule(ban.Value.IPAddress);
                    }
                }

                // Set up Windows Event Log monitoring
                _eventLog = new EventLog("Security");
                _eventLog.EntryWritten += OnSecurityEventWritten;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error: Administrator privileges are required to monitor security events.\n\n" +
                    "Please run the application as Administrator.",
                    "Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                throw;
            }
        }

        private void BanIP(string ipAddress)
        {
            if (!_bannedIPs.ContainsKey(ipAddress))
            {
                var duration = IsPrivateIP(ipAddress)
                    ? TimeSpan.FromHours(_settings.PrivateIPBanHours)
                    : TimeSpan.FromDays(_settings.PublicIPBanDays);

                var banInfo = new BanInfo
                {
                    IPAddress = ipAddress,
                    BanTime = DateTime.Now,
                    Duration = duration,
                    ExpiryTime = DateTime.Now.Add(duration),
                    AttemptCount = _loginAttempts[ipAddress].Count
                };

                _bannedIPs[ipAddress] = banInfo;
                AddFirewallRule(ipAddress);

                // Save banned IPs to file
                SettingsManager.SaveBannedIPs(_bannedIPs);

                // Raise the ban event
                IPBanned?.Invoke(this, new IPBanEventArgs
                {
                    IPAddress = ipAddress,
                    BanTime = DateTime.Now,
                    Duration = duration
                });

                _logger.LogInformation($"IP {ipAddress} banned for {duration.TotalHours:F1} hours");
            }
        }

        public void StartMonitoring()
        {
            if (!_isMonitoring)
            {
                _eventLog.EnableRaisingEvents = true;
                _isMonitoring = true;
                Debug.WriteLine("Monitoring started");
            }
        }

        public void StopMonitoring()
        {
            if (_isMonitoring)
            {
                _eventLog.EnableRaisingEvents = false;
                _isMonitoring = false;
                Debug.WriteLine("Monitoring stopped");
            }
        }

        private void OnSecurityEventWritten(object sender, EntryWrittenEventArgs e)
        {
            try
            {
                // RDP failed login event ID is 4625
                if (e.Entry.EventID == 4625)
                {
                    string ipAddress = ExtractIPAddress(e.Entry.Message);
                    if (!string.IsNullOrEmpty(ipAddress))
                    {
                        ProcessLoginAttempt(ipAddress);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing security event: {ex.Message}");
            }
        }

        private void ProcessLoginAttempt(string ipAddress)
        {
            // Skip if IP is whitelisted
            if (_settings.WhitelistedIPs.Any(w =>
                string.Equals(w.IPAddress, ipAddress, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            // Record the attempt
            if (!_loginAttempts.ContainsKey(ipAddress))
            {
                _loginAttempts[ipAddress] = new List<DateTime>();
            }
            _loginAttempts[ipAddress].Add(DateTime.Now);

            // Raise the event
            LoginAttemptDetected?.Invoke(this, new LoginAttemptEventArgs
            {
                IPAddress = ipAddress,
                Timestamp = DateTime.Now
            });

            // Check if we should ban this IP
            CheckForBan(ipAddress);
        }

        private void CheckForBan(string ipAddress)
        {
            if (_bannedIPs.ContainsKey(ipAddress))
                return;

            var recentAttempts = _loginAttempts[ipAddress]
                .Where(t => t > DateTime.Now.AddMinutes(-_settings.TimeWindow))
                .Count();

            if (recentAttempts >= _settings.MaxAttempts)
            {
                BanIP(ipAddress);
            }
        }



        private bool IsPrivateIP(string ipAddress)
        {
            if (IPAddress.TryParse(ipAddress, out IPAddress? ip))
            {
                byte[] bytes = ip.GetAddressBytes();
                return bytes[0] == 10 || // 10.x.x.x
                       (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) || // 172.16.x.x - 172.31.x.x
                       (bytes[0] == 192 && bytes[1] == 168); // 192.168.x.x
            }
            return false;
        }

        private string ExtractIPAddress(string logMessage)
        {
            try
            {
                // Very basic IP extraction - you might need to adjust this
                // based on your actual log format
                var match = System.Text.RegularExpressions.Regex.Match(
                    logMessage,
                    @"\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}\b"
                );

                if (match.Success)
                {
                    return match.Value;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error extracting IP: {ex.Message}");
            }

            return string.Empty;
        }

        private void AddFirewallRule(string ipAddress)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"advfirewall firewall add rule name=\"RDPSecure Block {ipAddress}\" dir=in interface=any action=block remoteip={ipAddress}",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }
                };
                process.Start();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding firewall rule: {ex.Message}");
            }
        }

        public void RemoveFirewallRule(string ipAddress)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"advfirewall firewall delete rule name=\"RDPSecure Block {ipAddress}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }
                };
                process.Start();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing firewall rule: {ex.Message}");
            }
        }

        public Dictionary<string, BanInfo> GetActiveBans()
        {
            return _bannedIPs
                .Where(kvp => DateTime.Now < kvp.Value.ExpiryTime)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value,
                    StringComparer.OrdinalIgnoreCase
                );
        }

    }


}