using System.Net;
using RDPSecure.Services;
using RDPSecure.Logging;
using System.Diagnostics;

namespace RDPSecure.Services
{
    public class RDPMonitorService : IRDPMonitorService
    {
        private const string FIREWALL_RULE_NAME = "RDPSecure-Blocked-IPs";
        private readonly AppSettings _settings;
        private readonly Dictionary<string, List<DateTime>> _loginAttempts;
        private readonly Dictionary<string, BanInfo> _bannedIPs;
        private bool _isMonitoring;
        private readonly EventLog _eventLog;
        private readonly ISecurityLogger _logger;

        public event EventHandler<LoginAttemptEventArgs>? LoginAttemptDetected;
        public event EventHandler<IPBanEventArgs>? IPBanned;



        private bool IsWhitelisted(string ipAddress)
        {
            // Reload settings to get the latest whitelist
            var currentSettings = SettingsManager.LoadSettings();
            return currentSettings.WhitelistedIPs.Any(w =>
                string.Equals(w.IPAddress, ipAddress, StringComparison.OrdinalIgnoreCase) &&
                w.IsEnabled);
        }


        public void AddManualBan(string ipAddress, TimeSpan duration)
        {
            // Check whitelist before manual ban
            if (IsWhitelisted(ipAddress))
            {
                MessageBox.Show(
                    $"Cannot ban {ipAddress} as it is whitelisted. Remove from whitelist first.",
                    "Whitelist Conflict",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!_bannedIPs.ContainsKey(ipAddress))
            {
                var banInfo = new BanInfo
                {
                    IPAddress = ipAddress,
                    BanTime = DateTime.Now,
                    Duration = duration,
                    ExpiryTime = DateTime.Now.Add(duration),
                    AttemptCount = 0
                };

                _bannedIPs[ipAddress] = banInfo;
                UpdateFirewallRule();
                SettingsManager.SaveBannedIPs(_bannedIPs);

                IPBanned?.Invoke(this, new IPBanEventArgs
                {
                    IPAddress = ipAddress,
                    BanTime = DateTime.Now,
                    Duration = duration
                });

                _logger.LogInformation($"IP {ipAddress} manually banned");
            }
        }


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
                }

                // Initialize or update the firewall rule
                UpdateFirewallRule();

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
            // Final whitelist check before banning
            if (IsWhitelisted(ipAddress))
            {
                _logger.LogInformation($"Prevented ban of whitelisted IP: {ipAddress}");
                return;
            }

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
                    AttemptCount = _loginAttempts.ContainsKey(ipAddress) ?
                        _loginAttempts[ipAddress].Count : 0
                };

                _bannedIPs[ipAddress] = banInfo;
                UpdateFirewallRule();
                SettingsManager.SaveBannedIPs(_bannedIPs);

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
            try
            {
                // First check whitelist - if whitelisted, log and return immediately
                if (IsWhitelisted(ipAddress))
                {
                    _logger.LogInformation($"Login attempt from whitelisted IP: {ipAddress} - allowing");
                    return;
                }

                // If IP is already banned, just log the attempt
                if (_bannedIPs.ContainsKey(ipAddress) && DateTime.Now < _bannedIPs[ipAddress].ExpiryTime)
                {
                    _logger.LogInformation($"Login attempt from banned IP: {ipAddress}");
                    return;
                }

                // Record the attempt for non-whitelisted IPs
                if (!_loginAttempts.ContainsKey(ipAddress))
                {
                    _loginAttempts[ipAddress] = new List<DateTime>();
                }

                // Add the attempt and clean old attempts
                _loginAttempts[ipAddress].Add(DateTime.Now);
                CleanOldAttempts(ipAddress);

                // Raise the event
                LoginAttemptDetected?.Invoke(this, new LoginAttemptEventArgs
                {
                    IPAddress = ipAddress,
                    Timestamp = DateTime.Now
                });

                // Check if we should ban this IP
                CheckForBan(ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing login attempt: {ex.Message}");
            }
        }

        private void CleanOldAttempts(string ipAddress)
        {
            if (_loginAttempts.ContainsKey(ipAddress))
            {
                var cutoffTime = DateTime.Now.AddMinutes(-_settings.TimeWindow);
                _loginAttempts[ipAddress] = _loginAttempts[ipAddress]
                    .Where(attempt => attempt > cutoffTime)
                    .ToList();
            }
        }

        public void RemoveBan(string ipAddress)
        {
            if (_bannedIPs.Remove(ipAddress))
            {
                UpdateFirewallRule();
                SettingsManager.SaveBannedIPs(_bannedIPs);
                _logger.LogInformation($"Ban removed for IP: {ipAddress}");
            }
        }

        private void UpdateFirewallRule()
        {
            try
            {
                // Remove existing rule if it exists
                RemoveFirewallRule();

                // Get all active banned IPs
                var activeBannedIPs = _bannedIPs
                    .Where(kvp => DateTime.Now < kvp.Value.ExpiryTime)
                    .Select(kvp => kvp.Key)
                    .ToList();

                if (activeBannedIPs.Any())
                {
                    // Create new rule with all banned IPs
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "netsh",
                            Arguments = $"advfirewall firewall add rule" +
                                      $" name=\"{FIREWALL_RULE_NAME}\"" +
                                      $" dir=in" +
                                      $" interface=any" +
                                      $" action=block" +
                                      $" remoteip={string.Join(",", activeBannedIPs)}",
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        }
                    };

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        _logger.LogError($"Error updating firewall rule. Output: {output} Error: {error}");
                    }
                    else
                    {
                        _logger.LogInformation($"Firewall rule updated with {activeBannedIPs.Count} IPs");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error managing firewall rule: {ex.Message}");
            }
        }

        private void RemoveFirewallRule()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"advfirewall firewall delete rule name=\"{FIREWALL_RULE_NAME}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing firewall rule: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _eventLog?.Dispose();
            // Don't remove the firewall rule on dispose - we want it to persist
        }



        private void CheckForBan(string ipAddress)
        {
            // Double check whitelist before banning
            if (IsWhitelisted(ipAddress))
            {
                _logger.LogInformation($"IP {ipAddress} is whitelisted - not banning");
                return;
            }

            var recentAttempts = _loginAttempts[ipAddress]
                .Where(t => t > DateTime.Now.AddMinutes(-_settings.TimeWindow))
                .Count();

            if (recentAttempts >= _settings.MaxAttempts)
            {
                BanIP(ipAddress);
            }
        }


        public void CleanupExpiredBans()
        {
            var expiredBans = _bannedIPs
                .Where(kvp => DateTime.Now >= kvp.Value.ExpiryTime)
                .Select(kvp => kvp.Key)
                .ToList();

            if (expiredBans.Any())
            {
                foreach (var ip in expiredBans)
                {
                    _bannedIPs.Remove(ip);
                }

                UpdateFirewallRule();
                SettingsManager.SaveBannedIPs(_bannedIPs);
                _logger.LogInformation($"Cleaned up {expiredBans.Count} expired bans");
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