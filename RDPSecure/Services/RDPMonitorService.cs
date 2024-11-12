using System.Net;
using RDPSecure.Services;
using RDPSecure.Logging;
using System.Diagnostics;
using RDPSecure.Data;

namespace RDPSecure.Services
{

    public class LoginAttempt
        {
            public string IPAddress { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }
    public class RDPMonitorService : IRDPMonitorService, IDisposable
    {
        private readonly System.Timers.Timer _cleanupTimer;
        private readonly IPLocationService _locationService;
        private const string FIREWALL_RULE_NAME = "RDPSecure-Blocked-IPs";
        private AppSettings _settings;
        private readonly object _settingsLock = new object();
        private readonly LoginAttemptsManager _attemptsManager;
        private readonly Dictionary<string, BanInfo> _bannedIPs;
        private bool _isMonitoring;
        private readonly EventLog _eventLog;
        private readonly ISecurityLogger _logger;
        public event EventHandler<IPLocationEventArgs>? IPLocationUpdated;
        public event EventHandler<LoginAttemptEventArgs>? LoginAttemptDetected;
        public event EventHandler<IPBanEventArgs>? IPBanned;




        public bool IsWhitelisted(string ipAddress)
        {
            try
            {
                // Reload settings to ensure we have the latest
                _settings = SettingsManager.LoadSettings();

                foreach (var whitelist in _settings.WhitelistedIPs.Where(w => w.IsEnabled))
                {
                    if (whitelist.MatchesIP(ipAddress))
                    {
                        _logger.LogInformation($"IP {ipAddress} matches whitelist entry: {whitelist.IPAddress}");
                        return true;
                    }
                }

                _logger.LogInformation($"IP {ipAddress} is not whitelisted");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking whitelist for IP {ipAddress}: {ex.Message}");
                return false;
            }
        }

        public List<LoginAttempt> GetRecentAttempts()
        {
            var attempts = new List<LoginAttempt>();
            var allAttempts = _attemptsManager.GetAllAttempts();

            foreach (var kvp in allAttempts)
            {
                foreach (var timestamp in kvp.Value)
                {
                    attempts.Add(new LoginAttempt
                    {
                        IPAddress = kvp.Key,
                        Timestamp = timestamp
                    });
                }
            }

            return attempts.OrderByDescending(a => a.Timestamp).ToList();
        }


        public void RefreshSettings()
        {
            lock (_settingsLock)
            {
                _settings = SettingsManager.LoadSettings();
                _logger.LogInformation("Settings refreshed");

                // Log current whitelist entries
                foreach (var entry in _settings.WhitelistedIPs)
                {
                    _logger.LogInformation($"Whitelist entry: {entry.IPAddress}, Enabled: {entry.IsEnabled}, IsSubnet: {entry.IsSubnet}");
                }
            }
        }



        public void AddManualBan(string ipAddress, TimeSpan duration)
        {
            try
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

                // Check if already banned
                if (_bannedIPs.ContainsKey(ipAddress))
                {
                    // If it's a "ghost" entry (not in JSON), remove it first
                    _bannedIPs.Remove(ipAddress);
                    SettingsManager.SaveBannedIPs(_bannedIPs);
                }

                var banInfo = new BanInfo
                {
                    IPAddress = ipAddress,
                    BanTime = DateTime.Now,
                    Duration = duration,
                    ExpiryTime = DateTime.Now.Add(duration),
                    AttemptCount = 0,
                    Location = IsPrivateIP(ipAddress) ? "Private" : "Detecting..."  // Set Private immediately for private IPs
                };

                lock (_bannedIPs)
                {
                    _bannedIPs[ipAddress] = banInfo;
                    // Save to JSON immediately
                    SettingsManager.SaveBannedIPs(_bannedIPs);
                    UpdateFirewallRule();
                }

                // Raise the ban event
                IPBanned?.Invoke(this, new IPBanEventArgs
                {
                    IPAddress = ipAddress,
                    BanTime = DateTime.Now,
                    Duration = duration
                });

                // Start location lookup
                _ = UpdateLocationForIP(ipAddress);
                _logger.LogInformation($"IP {ipAddress} manually banned for {duration.TotalDays:F1} days");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding manual ban for IP {ipAddress}: {ex.Message}");
                MessageBox.Show(
                    $"An error occurred while banning {ipAddress}. Please try again.",
                    "Ban Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public RDPMonitorService(AppSettings settings)
        {
            try
            {
                // Initialize logger first
                _logger = new SecurityLogger();

                _settings = settings;
                //_loginAttempts = new Dictionary<string, List<DateTime>>(StringComparer.OrdinalIgnoreCase);
                _bannedIPs = new Dictionary<string, BanInfo>(StringComparer.OrdinalIgnoreCase);
               
                // Initialize the attempts manager
                _attemptsManager = new LoginAttemptsManager(_logger, settings.TimeWindow);

                // Initialize location service with the guaranteed non-null logger
                _locationService = new IPLocationService(_logger);
                                
                _cleanupTimer = new System.Timers.Timer(60000);
                _cleanupTimer.Elapsed += (s, e) => CleanupBannedIPs();
                _cleanupTimer.Start();

                var savedBans = SettingsManager.LoadBannedIPs();
                foreach (var ban in savedBans)
                {
                    _bannedIPs[ban.Key] = ban.Value;
                }

                UpdateLocationsForExistingBans();
                UpdateFirewallRule();

                _eventLog = new EventLog("Security");
                _eventLog.EntryWritten += OnSecurityEventWritten;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error initializing RDPMonitorService: {ex.Message}");
                throw;
            }
        }

        private async void UpdateLocationsForExistingBans()
        {
            foreach (var ban in _bannedIPs.Values)
            {
                if (ban.Location == "Detecting..." || ban.Location == "Unknown")
                {
                    await UpdateLocationForIP(ban.IPAddress);
                }
            }
        }
        public int GetRecentAttemptsCount()
        {
            try
            {
                return _attemptsManager.GetTotalAttempts(TimeSpan.FromHours(24));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting recent attempts count: {ex.Message}");
                return 0;
            }
        }

        private async Task UpdateLocationForIP(string ipAddress)
        {
            try
            {
                if (_bannedIPs.TryGetValue(ipAddress, out var banInfo))
                {
                    // Check if it's a private IP
                    if (IsPrivateIP(ipAddress))
                    {
                        banInfo.Location = "Private";
                    }
                    else
                    {
                        string location = await _locationService.GetIPLocation(ipAddress);
                        banInfo.Location = location;
                    }

                    // Notify UI of location update
                    IPLocationUpdated?.Invoke(this, new IPLocationEventArgs
                    {
                        IPAddress = ipAddress,
                        Location = banInfo.Location
                    });

                    // Save updated ban info
                    SettingsManager.SaveBannedIPs(_bannedIPs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating location for IP {ipAddress}: {ex.Message}");
            }
        }

        private void BanIP(string ipAddress)
        {
            try
            {
                _logger.LogInformation($"Starting ban process for IP: {ipAddress}");

                // Final whitelist check before banning
                if (IsWhitelisted(ipAddress))
                {
                    _logger.LogInformation($"Prevented ban of whitelisted IP: {ipAddress}");
                    return;
                }

                lock (_bannedIPs)
                {
                    // Remove existing ban if present
                    if (_bannedIPs.ContainsKey(ipAddress))
                    {
                        _bannedIPs.Remove(ipAddress);
                    }

                    // Calculate ban duration
                    var duration = IsPrivateIP(ipAddress)
                        ? TimeSpan.FromHours(_settings.PrivateIPBanHours)
                        : TimeSpan.FromDays(_settings.PublicIPBanDays);

                    var banInfo = new BanInfo
                    {
                        IPAddress = ipAddress,
                        BanTime = DateTime.Now,
                        Duration = duration,
                        ExpiryTime = DateTime.Now.Add(duration),
                        AttemptCount = _attemptsManager.GetRecentAttemptCount(ipAddress),
                        Location = IsPrivateIP(ipAddress) ? "Private" : "Detecting..."  // Set Private immediately for private IPs
                    };

                    // Add the ban
                    _bannedIPs[ipAddress] = banInfo;

                    // Save to JSON
                    try
                    {
                        SettingsManager.SaveBannedIPs(_bannedIPs);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to save banned IPs, but IP {ipAddress} is still banned in memory: {ex.Message}");
                    }                    
                    UpdateFirewallRule();

                    _logger.LogInformation(
                        $"IP {ipAddress} banned successfully. " +
                        $"Duration: {duration.TotalHours:F1} hours. " +
                        $"Expiry: {banInfo.ExpiryTime}"
                    );

                    // Raise the ban event
                    IPBanned?.Invoke(this, new IPBanEventArgs
                    {
                        IPAddress = ipAddress,
                        BanTime = DateTime.Now,
                        Duration = duration
                    });
                    // Start location lookup
                    _ = UpdateLocationForIP(ipAddress);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error banning IP {ipAddress}: {ex.Message}");
                throw;
            }
        }

        public bool IsIPBanned(string ipAddress)
        {
            // Check both memory and file
            var bannedIPs = SettingsManager.LoadBannedIPs();
            return _bannedIPs.ContainsKey(ipAddress) || bannedIPs.ContainsKey(ipAddress);
        }

        public void CleanupBannedIPs()
        {
            try
            {
                // Load from file
                var savedBans = SettingsManager.LoadBannedIPs();

                // Synchronize memory with file
                _bannedIPs.Clear();
                foreach (var ban in savedBans)
                {
                    _bannedIPs[ban.Key] = ban.Value;
                }                
                UpdateFirewallRule();

                _logger.LogInformation("Banned IPs cleaned up and synchronized with file");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error cleaning up banned IPs", ex);
                throw;
            }
        }

        public void StartMonitoring()
        {
            if (!_isMonitoring)
            {
                _eventLog.EnableRaisingEvents = true;
                _cleanupTimer.Start();  // Start the cleanup timer
                _isMonitoring = true;
                _logger.LogInformation("Monitoring started");
            }
        }

        public void StopMonitoring()
        {
            if (_isMonitoring)
            {
                _eventLog.EnableRaisingEvents = false;
                _cleanupTimer.Stop();  // Stop the cleanup timer
                _isMonitoring = false;
                _logger.LogInformation("Monitoring stopped");
            }
        }

        private void OnSecurityEventWritten(object sender, EntryWrittenEventArgs e)
        {
            try
            {
                // RDP failed login event ID 4625
                if (e.Entry.InstanceId == 4625)
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
                _logger.LogInformation($"Processing login attempt from IP: {ipAddress}");

                // Record the attempt
                _attemptsManager.AddAttempt(ipAddress, DateTime.Now);

                // Always raise the LoginAttemptDetected event, even for whitelisted IPs
                LoginAttemptDetected?.Invoke(this, new LoginAttemptEventArgs
                {
                    IPAddress = ipAddress,
                    Timestamp = DateTime.Now
                });
                // Check whitelist after raising the event
                if (IsWhitelisted(ipAddress))
                {
                    _logger.LogInformation($"Login attempt from whitelisted IP: {ipAddress} - allowing");
                    return;
                }

                // Check if IP is currently banned
                if (_bannedIPs.TryGetValue(ipAddress, out var banInfo))
                {
                    if (DateTime.Now < banInfo.ExpiryTime)
                    {
                        _logger.LogInformation($"Blocked attempt from banned IP: {ipAddress}");
                        LoginAttemptDetected?.Invoke(this, new LoginAttemptEventArgs
                        {
                            IPAddress = ipAddress,
                            Timestamp = DateTime.Now
                        });
                        return;
                    }

                    // Remove expired ban
                    _bannedIPs.Remove(ipAddress);
                    UpdateFirewallRule();
                }

                // Record the attempt
                _attemptsManager.AddAttempt(ipAddress, DateTime.Now);

                // Get current count within time window
                int recentAttempts = _attemptsManager.GetRecentAttemptCount(ipAddress);

                _logger.LogInformation(
                    $"IP: {ipAddress} - Attempt {recentAttempts} of {_settings.MaxAttempts} allowed " +
                    $"(Window: {_settings.TimeWindow} minutes)"
                );

                // Raise the LoginAttemptDetected event
                LoginAttemptDetected?.Invoke(this, new LoginAttemptEventArgs
                {
                    IPAddress = ipAddress,
                    Timestamp = DateTime.Now
                });

                // Check if attempts exceed threshold
                if (recentAttempts >= _settings.MaxAttempts)
                {
                    _logger.LogInformation(
                        $"IP {ipAddress} has exceeded maximum attempts " +
                        $"({recentAttempts}/{_settings.MaxAttempts}). Initiating ban..."
                    );
                    BanIP(ipAddress);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing login attempt for IP {ipAddress}: {ex.Message}");
            }
        }

        public void RemoveBan(string ipAddress)
        {
            try
            {
                // Remove from banned IPs dictionary
                if (_bannedIPs.Remove(ipAddress))
                {
                    // Remove from login attempts tracking
                    _attemptsManager.RemoveAttempts(ipAddress);

                    // Update firewall and save changes
                    UpdateFirewallRule();
                    SettingsManager.SaveBannedIPs(_bannedIPs);

                    _logger.LogInformation($"Ban removed for IP: {ipAddress}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing ban for IP {ipAddress}: {ex.Message}");
                throw;
            }
        }

        private void UpdateFirewallRule()
        {
            try
            {
                // Remove existing rules
                RemoveFirewallRule("IPv4");
                RemoveFirewallRule("IPv6");

                // Get all active banned IPs
                var activeBans = _bannedIPs
                    .Where(kvp => DateTime.Now < kvp.Value.ExpiryTime)
                    .ToList();

                // Separate IPv4 and IPv6 addresses
                var ipv4Bans = activeBans
                    .Where(b => !b.Value.IsIPv6)
                    .Select(b => b.Key)
                    .ToList();

                var ipv6Bans = activeBans
                    .Where(b => b.Value.IsIPv6)
                    .Select(b => b.Key)
                    .ToList();

                // Create IPv4 rule if needed
                if (ipv4Bans.Any())
                {
                    CreateFirewallRule(ipv4Bans, "IPv4");
                }

                // Create IPv6 rule if needed
                if (ipv6Bans.Any())
                {
                    CreateFirewallRule(ipv6Bans, "IPv6");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error managing firewall rules: {ex.Message}");
            }
        }

        private void CreateFirewallRule(List<string> ips, string version)
        {
            var ruleName = $"{FIREWALL_RULE_NAME}-{version}";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall add rule" +
                               $" name=\"{ruleName}\"" +
                               $" dir=in" +
                               $" interface=any" +
                               $" action=block" +
                               $" remoteip={string.Join(",", ips)}" +
                               (version == "IPv6" ? " protocol=IPv6" : ""),
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
                _logger.LogError($"Error creating {version} firewall rule. Output: {output} Error: {error}");
            }
            else
            {
                _logger.LogInformation($"{version} firewall rule created with {ips.Count} IPs");
            }
        }

        private void RemoveFirewallRule(string version)
        {
            try
            {
                var ruleName = $"{FIREWALL_RULE_NAME}-{version}";
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"advfirewall firewall delete rule name=\"{ruleName}\"",
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
                _logger.LogError($"Error removing {version} firewall rule: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _eventLog?.Dispose();
            _cleanupTimer?.Dispose();
            _attemptsManager?.Dispose();
        }
        public int GetTotalRecentAttempts()
        {
            return _attemptsManager.GetTotalAttempts(TimeSpan.FromHours(24));
        }

        public void CleanupExpiredBans()
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError($"Error cleaning up expired bans: {ex.Message}");
            }
        }

        private bool IsPrivateIP(string ipAddress)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError($"Error checking if IP {ipAddress} is private: {ex.Message}");
                return false;
            }
        }

        private string ExtractIPAddress(string logMessage)
        {
            try
            {  
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

        public class IPLocationEventArgs : EventArgs
        {
            public string IPAddress { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
        }

        public void OnSettingsChanged()
        {
            try
            {
                var newSettings = SettingsManager.LoadSettings();

                // If time window changed, we need to reinitialize the attempts manager
                if (newSettings.TimeWindow != _settings.TimeWindow)
                {
                    var oldManager = _attemptsManager;
                    // Create new manager with new time window
                    var newManager = new LoginAttemptsManager(_logger, newSettings.TimeWindow);

                    // Update the field
                    //_attemptsManager = newManager;

                    // Dispose old manager
                    oldManager.Dispose();
                }

                _settings = newSettings;
                _logger.LogInformation("Settings updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating settings: {ex.Message}");
            }
        }

    }

}