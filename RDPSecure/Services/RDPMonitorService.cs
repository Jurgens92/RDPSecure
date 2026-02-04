using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
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
        private DateTime _settingsLastLoaded = DateTime.MinValue;
        private static readonly TimeSpan SettingsCacheTimeout = TimeSpan.FromSeconds(5);
        private LoginAttemptsManager _attemptsManager;
        private readonly ConcurrentDictionary<string, BanInfo> _bannedIPs;
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
                // Use cached settings with timeout to avoid DB query on every check
                EnsureSettingsLoaded();

                foreach (var whitelist in _settings.WhitelistedIPs.Where(w => w.IsEnabled))
                {
                    if (whitelist.MatchesIP(ipAddress))
                    {
                        _logger.LogInformation($"IP {ipAddress} matches whitelist entry: {whitelist.IPAddress}");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking whitelist for IP {ipAddress}: {ex.Message}");
                return false;
            }
        }

        public bool IsBlacklisted(string ipAddress)
        {
            try
            {
                EnsureSettingsLoaded();

                foreach (var blacklist in _settings.BlacklistedIPs.Where(b => b.IsEnabled))
                {
                    if (blacklist.MatchesIP(ipAddress))
                    {
                        _logger.LogInformation($"IP {ipAddress} matches blacklist entry: {blacklist.IPAddress}");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking blacklist for IP {ipAddress}: {ex.Message}");
                return false;
            }
        }

        private void EnsureSettingsLoaded()
        {
            lock (_settingsLock)
            {
                if (DateTime.UtcNow - _settingsLastLoaded > SettingsCacheTimeout)
                {
                    _settings = SettingsManager.LoadSettings();
                    _settingsLastLoaded = DateTime.UtcNow;
                }
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

                // Remove existing ban if present (using ConcurrentDictionary)
                _bannedIPs.TryRemove(ipAddress, out _);

                var now = DateTime.UtcNow;
                var banInfo = new BanInfo
                {
                    IPAddress = ipAddress,
                    BanTime = now,
                    Duration = duration,
                    ExpiryTime = now.Add(duration),
                    AttemptCount = 0,
                    Location = IsPrivateIP(ipAddress) ? "Private" : "Detecting..."
                };

                // Use ConcurrentDictionary's thread-safe operations
                _bannedIPs[ipAddress] = banInfo;
                SaveBannedIPsAndUpdateFirewall();

                // Raise the ban event
                IPBanned?.Invoke(this, new IPBanEventArgs
                {
                    IPAddress = ipAddress,
                    BanTime = now,
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

        private void SaveBannedIPsAndUpdateFirewall()
        {
            try
            {
                SettingsManager.SaveBannedIPs(new Dictionary<string, BanInfo>(_bannedIPs));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save banned IPs: {ex.Message}");
            }
            UpdateFirewallRule();
        }

        public RDPMonitorService(AppSettings settings)
        {
            try
            {
                // Initialize logger first
                _logger = new SecurityLogger();

                _settings = settings;
                _settingsLastLoaded = DateTime.UtcNow;
                _bannedIPs = new ConcurrentDictionary<string, BanInfo>(StringComparer.OrdinalIgnoreCase);

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
                ReloadAttempts();  
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

                // Remove existing ban if present (thread-safe)
                _bannedIPs.TryRemove(ipAddress, out _);

                // Calculate ban duration
                var duration = IsPrivateIP(ipAddress)
                    ? TimeSpan.FromHours(_settings.PrivateIPBanHours)
                    : TimeSpan.FromDays(_settings.PublicIPBanDays);

                var now = DateTime.UtcNow;
                var banInfo = new BanInfo
                {
                    IPAddress = ipAddress,
                    BanTime = now,
                    Duration = duration,
                    ExpiryTime = now.Add(duration),
                    AttemptCount = _attemptsManager.GetRecentAttemptCount(ipAddress),
                    Location = IsPrivateIP(ipAddress) ? "Private" : "Detecting..."
                };

                // Add the ban (thread-safe)
                _bannedIPs[ipAddress] = banInfo;

                // Save and update firewall
                SaveBannedIPsAndUpdateFirewall();

                _logger.LogInformation(
                    $"IP {ipAddress} banned successfully. " +
                    $"Duration: {duration.TotalHours:F1} hours. " +
                    $"Expiry: {banInfo.ExpiryTime}"
                );

                // Raise the ban event
                IPBanned?.Invoke(this, new IPBanEventArgs
                {
                    IPAddress = ipAddress,
                    BanTime = now,
                    Duration = duration
                });

                // Start location lookup
                _ = UpdateLocationForIP(ipAddress);
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
                var now = DateTime.UtcNow;

                // Merge saved bans with current in-memory bans (don't clear to avoid losing concurrent additions)
                foreach (var ban in savedBans)
                {
                    // Only add if not already in memory or if the saved version is newer
                    _bannedIPs.AddOrUpdate(
                        ban.Key,
                        ban.Value,
                        (key, existing) => ban.Value.BanTime > existing.BanTime ? ban.Value : existing
                    );
                }

                // Remove expired bans
                var expiredKeys = _bannedIPs
                    .Where(kvp => now >= kvp.Value.ExpiryTime)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _bannedIPs.TryRemove(key, out _);
                }

                if (expiredKeys.Any())
                {
                    // Save after removing expired bans
                    try
                    {
                        SettingsManager.SaveBannedIPs(new Dictionary<string, BanInfo>(_bannedIPs));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to save banned IPs after cleanup: {ex.Message}");
                    }
                }

                UpdateFirewallRule();

                _logger.LogInformation($"Banned IPs cleaned up. Removed {expiredKeys.Count} expired bans.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error cleaning up banned IPs", ex);
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

                var now = DateTime.UtcNow;

                // Record the attempt first
                _attemptsManager.AddAttempt(ipAddress, now);

                // Always raise the LoginAttemptDetected event, even for whitelisted IPs
                LoginAttemptDetected?.Invoke(this, new LoginAttemptEventArgs
                {
                    IPAddress = ipAddress,
                    Timestamp = now
                });

                // Check whitelist - allow but still track the attempt
                if (IsWhitelisted(ipAddress))
                {
                    _logger.LogInformation($"Login attempt from whitelisted IP: {ipAddress} - allowing");
                    return;
                }

                // Check blacklist - immediately ban blacklisted IPs
                if (IsBlacklisted(ipAddress))
                {
                    _logger.LogInformation($"Login attempt from blacklisted IP: {ipAddress} - banning immediately");
                    if (!_bannedIPs.ContainsKey(ipAddress))
                    {
                        // Ban for the configured public IP duration (blacklisted IPs are always treated as threats)
                        var duration = TimeSpan.FromDays(_settings.PublicIPBanDays);
                        var banInfo = new BanInfo
                        {
                            IPAddress = ipAddress,
                            BanTime = now,
                            Duration = duration,
                            ExpiryTime = now.Add(duration),
                            AttemptCount = 1,
                            Location = IsPrivateIP(ipAddress) ? "Private" : "Detecting..."
                        };
                        _bannedIPs[ipAddress] = banInfo;
                        SaveBannedIPsAndUpdateFirewall();

                        IPBanned?.Invoke(this, new IPBanEventArgs
                        {
                            IPAddress = ipAddress,
                            BanTime = now,
                            Duration = duration
                        });

                        _ = UpdateLocationForIP(ipAddress);
                    }
                    return;
                }

                // Check if IP is currently banned
                if (_bannedIPs.TryGetValue(ipAddress, out var existingBan))
                {
                    if (now < existingBan.ExpiryTime)
                    {
                        _logger.LogInformation($"Blocked attempt from banned IP: {ipAddress}");
                        return;
                    }

                    // Remove expired ban (thread-safe)
                    _bannedIPs.TryRemove(ipAddress, out _);
                    UpdateFirewallRule();
                }

                // Get current count within time window
                int recentAttempts = _attemptsManager.GetRecentAttemptCount(ipAddress);

                _logger.LogInformation(
                    $"IP: {ipAddress} - Attempt {recentAttempts} of {_settings.MaxAttempts} allowed " +
                    $"(Window: {_settings.TimeWindow} minutes)"
                );

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
        private async Task SaveAttemptsToFileAsync()
        {
            try
            {
                await _attemptsManager.SaveAttemptsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving attempts: {ex.Message}");
            }
        }
        public void ReloadAttempts()
        {
            try
            {

                _attemptsManager.LoadAttempts();  // Make sure this method is public in LoginAttemptsManager
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reloading attempts: {ex.Message}");
            }
        }

        public void RemoveBan(string ipAddress)
        {
            try
            {
                // Remove from banned IPs dictionary (thread-safe)
                if (_bannedIPs.TryRemove(ipAddress, out _))
                {
                    // Remove from login attempts tracking
                    _attemptsManager.RemoveAttempts(ipAddress);

                    // Update firewall and save changes
                    UpdateFirewallRule();
                    SettingsManager.SaveBannedIPs(new Dictionary<string, BanInfo>(_bannedIPs));

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
                var now = DateTime.UtcNow;
                var activeBans = _bannedIPs
                    .Where(kvp => now < kvp.Value.ExpiryTime)
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
                var now = DateTime.UtcNow;
                var expiredBans = _bannedIPs
                    .Where(kvp => now >= kvp.Value.ExpiryTime)
                    .Select(kvp => kvp.Key)
                    .ToList();

                if (expiredBans.Any())
                {
                    foreach (var ip in expiredBans)
                    {
                        _bannedIPs.TryRemove(ip, out _);
                    }

                    UpdateFirewallRule();
                    SettingsManager.SaveBannedIPs(new Dictionary<string, BanInfo>(_bannedIPs));
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
                    // Handle IPv4
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        byte[] bytes = ip.GetAddressBytes();
                        return bytes[0] == 10 || // 10.x.x.x
                               (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) || // 172.16.x.x - 172.31.x.x
                               (bytes[0] == 192 && bytes[1] == 168) || // 192.168.x.x
                               bytes[0] == 127; // 127.x.x.x loopback
                    }

                    // Handle IPv6
                    if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        // Check for loopback (::1)
                        if (IPAddress.IsLoopback(ip))
                            return true;

                        byte[] bytes = ip.GetAddressBytes();

                        // Link-local addresses (fe80::/10)
                        if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80)
                            return true;

                        // Unique local addresses (fc00::/7 - includes fd00::/8)
                        if ((bytes[0] & 0xFE) == 0xFC)
                            return true;

                        // Site-local addresses (deprecated but still private) (fec0::/10)
                        if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0xC0)
                            return true;

                        return false;
                    }
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
                // IPv4 pattern
                var ipv4Match = System.Text.RegularExpressions.Regex.Match(
                    logMessage,
                    @"\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}\b"
                );

                if (ipv4Match.Success)
                {
                    return ipv4Match.Value;
                }

                // IPv6 pattern (handles full, compressed, and mixed notations)
                var ipv6Match = System.Text.RegularExpressions.Regex.Match(
                    logMessage,
                    @"\b(?:[0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}\b|" +  // Full notation
                    @"\b(?:[0-9a-fA-F]{1,4}:){1,7}:\b|" +               // Ending with ::
                    @"\b:(?::[0-9a-fA-F]{1,4}){1,7}\b|" +               // Starting with ::
                    @"\b(?:[0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}\b|" + // :: in middle
                    @"\b(?:[0-9a-fA-F]{1,4}:){1,5}(?::[0-9a-fA-F]{1,4}){1,2}\b|" +
                    @"\b(?:[0-9a-fA-F]{1,4}:){1,4}(?::[0-9a-fA-F]{1,4}){1,3}\b|" +
                    @"\b(?:[0-9a-fA-F]{1,4}:){1,3}(?::[0-9a-fA-F]{1,4}){1,4}\b|" +
                    @"\b(?:[0-9a-fA-F]{1,4}:){1,2}(?::[0-9a-fA-F]{1,4}){1,5}\b|" +
                    @"\b[0-9a-fA-F]{1,4}:(?::[0-9a-fA-F]{1,4}){1,6}\b|" +
                    @"\b::(?:[0-9a-fA-F]{1,4}:){0,5}[0-9a-fA-F]{1,4}\b|" + // :: followed by address
                    @"\b::(?:ffff:)?(?:[0-9]{1,3}\.){3}[0-9]{1,3}\b"       // IPv4-mapped IPv6
                );

                if (ipv6Match.Success)
                {
                    // Validate and normalize the IPv6 address
                    if (IPAddress.TryParse(ipv6Match.Value, out var parsedIP) &&
                        parsedIP.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        return parsedIP.ToString();
                    }
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
            var now = DateTime.UtcNow;
            return _bannedIPs
                .Where(kvp => now < kvp.Value.ExpiryTime)
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
                    _attemptsManager = newManager;

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