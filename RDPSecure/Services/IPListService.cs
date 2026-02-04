using RDPSecure.Logging;

namespace RDPSecure.Services
{
    /// <summary>
    /// Represents the type of IP list.
    /// </summary>
    public enum IPListType
    {
        Whitelist,
        Blacklist
    }

    /// <summary>
    /// Result of an IP list check.
    /// </summary>
    public class IPListCheckResult
    {
        public bool IsMatch { get; set; }
        public IPListType ListType { get; set; }
        public string? MatchedEntry { get; set; }
    }

    /// <summary>
    /// Service for checking IPs against whitelist and blacklist.
    /// Consolidates IP list management logic.
    /// </summary>
    public class IPListService
    {
        private readonly ISecurityLogger _logger;
        private AppSettings _settings;
        private readonly object _settingsLock = new object();
        private DateTime _settingsLastLoaded = DateTime.MinValue;
        private static readonly TimeSpan SettingsCacheTimeout = TimeSpan.FromSeconds(5);

        public IPListService(ISecurityLogger logger, AppSettings settings)
        {
            _logger = logger;
            _settings = settings;
            _settingsLastLoaded = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if an IP is in the whitelist.
        /// </summary>
        public bool IsWhitelisted(string ipAddress)
        {
            return CheckIPList(ipAddress, IPListType.Whitelist).IsMatch;
        }

        /// <summary>
        /// Checks if an IP is in the blacklist.
        /// </summary>
        public bool IsBlacklisted(string ipAddress)
        {
            return CheckIPList(ipAddress, IPListType.Blacklist).IsMatch;
        }

        /// <summary>
        /// Checks an IP against the specified list type.
        /// </summary>
        public IPListCheckResult CheckIPList(string ipAddress, IPListType listType)
        {
            try
            {
                EnsureSettingsLoaded();

                var entries = listType == IPListType.Whitelist
                    ? _settings.WhitelistedIPs.Where(w => w.IsEnabled)
                    : _settings.BlacklistedIPs.Where(b => b.IsEnabled);

                foreach (var entry in entries)
                {
                    if (entry.MatchesIP(ipAddress))
                    {
                        _logger.LogInformation(
                            $"IP {ipAddress} matches {listType.ToString().ToLower()} entry: {entry.IPAddress}");
                        return new IPListCheckResult
                        {
                            IsMatch = true,
                            ListType = listType,
                            MatchedEntry = entry.IPAddress
                        };
                    }
                }

                return new IPListCheckResult { IsMatch = false, ListType = listType };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking {listType.ToString().ToLower()} for IP {ipAddress}: {ex.Message}");
                return new IPListCheckResult { IsMatch = false, ListType = listType };
            }
        }

        /// <summary>
        /// Checks IP against both lists. Returns whitelist result if whitelisted,
        /// otherwise returns blacklist result.
        /// </summary>
        public IPListCheckResult CheckAllLists(string ipAddress)
        {
            var whitelistResult = CheckIPList(ipAddress, IPListType.Whitelist);
            if (whitelistResult.IsMatch)
                return whitelistResult;

            return CheckIPList(ipAddress, IPListType.Blacklist);
        }

        /// <summary>
        /// Refreshes settings from storage.
        /// </summary>
        public void RefreshSettings()
        {
            lock (_settingsLock)
            {
                _settings = SettingsManager.LoadSettings();
                _settingsLastLoaded = DateTime.UtcNow;
                _logger.LogInformation("IP list settings refreshed");

                foreach (var entry in _settings.WhitelistedIPs)
                {
                    _logger.LogInformation(
                        $"Whitelist entry: {entry.IPAddress}, Enabled: {entry.IsEnabled}, IsSubnet: {entry.IsSubnet}");
                }
            }
        }

        /// <summary>
        /// Gets the current settings.
        /// </summary>
        public AppSettings GetSettings()
        {
            EnsureSettingsLoaded();
            return _settings;
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
    }
}
