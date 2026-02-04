using Microsoft.Data.Sqlite;
using RDPSecure.Logging;

namespace RDPSecure.Data
{
    public class DatabaseManager : IDisposable
    {
        private static readonly string DbPath = Path.Combine(AppConfig.AppDataPath, "rdpsecure.db");
        private readonly SqliteConnection _connection;
        private readonly object _lock = new object();
        private bool _disposed;

        public DatabaseManager()
        {
            AppConfig.EnsureDirectoriesExist();
            _connection = new SqliteConnection($"Data Source={DbPath}");
            _connection.Open();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS BannedIPs (
                    IPAddress TEXT PRIMARY KEY,
                    BanTime TEXT NOT NULL,
                    Duration TEXT NOT NULL,
                    ExpiryTime TEXT NOT NULL,
                    AttemptCount INTEGER NOT NULL,
                    Location TEXT NOT NULL,
                    Version INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS LoginAttempts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    IPAddress TEXT NOT NULL,
                    Timestamp TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS WhitelistedIPs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    IPAddress TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    AddedDate TEXT NOT NULL,
                    IsEnabled INTEGER NOT NULL,
                    Notes TEXT,
                    IsSubnet INTEGER NOT NULL,
                    PrefixLength INTEGER,
                    NetworkAddress TEXT,
                    IsIPv6 INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS BlacklistedIPs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    IPAddress TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    AddedDate TEXT NOT NULL,
                    IsEnabled INTEGER NOT NULL,
                    Notes TEXT,
                    IsSubnet INTEGER NOT NULL,
                    PrefixLength INTEGER,
                    NetworkAddress TEXT,
                    IsIPv6 INTEGER NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_login_attempts_ip ON LoginAttempts(IPAddress);
                CREATE INDEX IF NOT EXISTS idx_login_attempts_timestamp ON LoginAttempts(Timestamp);
                CREATE INDEX IF NOT EXISTS idx_banned_ips_expiry ON BannedIPs(ExpiryTime);
            ";
            command.ExecuteNonQuery();

            // Migrate existing JSON data if it exists
            MigrateFromJson();
        }

        private void MigrateFromJson()
        {
            try
            {
                // Check if we've already migrated
                using var checkCommand = _connection.CreateCommand();
                checkCommand.CommandText = "SELECT COUNT(*) FROM Settings WHERE Key = 'migrated_from_json'";
                var count = Convert.ToInt64(checkCommand.ExecuteScalar());
                if (count > 0) return;

                // Migrate settings
                var settingsPath = Path.Combine(AppConfig.AppDataPath, "settings.json");
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettings>(json);
                    if (settings != null)
                    {
                        SaveSetting("MaxAttempts", settings.MaxAttempts.ToString());
                        SaveSetting("TimeWindow", settings.TimeWindow.ToString());
                        SaveSetting("PrivateIPBanHours", settings.PrivateIPBanHours.ToString());
                        SaveSetting("PublicIPBanDays", settings.PublicIPBanDays.ToString());
                        SaveSetting("GlobalBanlistEnabled", settings.GlobalBanlistEnabled.ToString());
                        SaveSetting("GitHub_AccessToken", settings.GitHub?.AccessToken ?? "");
                        SaveSetting("GitHub_RefreshInterval", (settings.GitHub?.RefreshInterval ?? 30).ToString());
                        SaveSetting("GitHub_EnableRateLimitProtection", (settings.GitHub?.EnableRateLimitProtection ?? true).ToString());

                        foreach (var ip in settings.WhitelistedIPs)
                        {
                            AddWhitelistedIP(ip);
                        }

                        foreach (var ip in settings.BlacklistedIPs)
                        {
                            AddBlacklistedIP(ip);
                        }
                    }
                }

                // Migrate banned IPs
                var bannedPath = Path.Combine(AppConfig.AppDataPath, "banned_ips.json");
                if (File.Exists(bannedPath))
                {
                    var json = File.ReadAllText(bannedPath);
                    var bannedIPs = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, BanInfo>>(json);
                    if (bannedIPs != null)
                    {
                        foreach (var kvp in bannedIPs)
                        {
                            SaveBannedIP(kvp.Value);
                        }
                    }
                }

                // Migrate login attempts
                var attemptsPath = Path.Combine(AppConfig.AppDataPath, "login_attempts.json");
                if (File.Exists(attemptsPath))
                {
                    var json = File.ReadAllText(attemptsPath);
                    var attempts = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<DateTime>>>(json);
                    if (attempts != null)
                    {
                        foreach (var kvp in attempts)
                        {
                            foreach (var timestamp in kvp.Value)
                            {
                                AddLoginAttempt(kvp.Key, timestamp);
                            }
                        }
                    }
                }

                // Mark as migrated
                SaveSetting("migrated_from_json", DateTime.UtcNow.ToString("O"));
            }
            catch (Exception)
            {
                // If migration fails, continue without it - the app will start fresh
            }
        }

        #region Settings Operations

        public void SaveSetting(string key, string value)
        {
            lock (_lock)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = @"
                    INSERT OR REPLACE INTO Settings (Key, Value) VALUES (@key, @value)
                ";
                command.Parameters.AddWithValue("@key", key);
                command.Parameters.AddWithValue("@value", value);
                command.ExecuteNonQuery();
            }
        }

        public string? GetSetting(string key)
        {
            lock (_lock)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = "SELECT Value FROM Settings WHERE Key = @key";
                command.Parameters.AddWithValue("@key", key);
                return command.ExecuteScalar()?.ToString();
            }
        }

        public AppSettings LoadSettings()
        {
            var settings = new AppSettings
            {
                MaxAttempts = int.TryParse(GetSetting("MaxAttempts"), out var max) ? max : 3,
                TimeWindow = int.TryParse(GetSetting("TimeWindow"), out var tw) ? tw : 5,
                PrivateIPBanHours = int.TryParse(GetSetting("PrivateIPBanHours"), out var priv) ? priv : 1,
                PublicIPBanDays = int.TryParse(GetSetting("PublicIPBanDays"), out var pub) ? pub : 30,
                GlobalBanlistEnabled = bool.TryParse(GetSetting("GlobalBanlistEnabled"), out var gbl) && gbl,
                GitHub = new GitHubSettings
                {
                    AccessToken = GetSetting("GitHub_AccessToken") ?? "",
                    RefreshInterval = int.TryParse(GetSetting("GitHub_RefreshInterval"), out var ri) ? ri : 30,
                    EnableRateLimitProtection = !bool.TryParse(GetSetting("GitHub_EnableRateLimitProtection"), out var rlp) || rlp
                },
                WhitelistedIPs = GetWhitelistedIPs(),
                BlacklistedIPs = GetBlacklistedIPs()
            };

            return settings;
        }

        public void SaveSettings(AppSettings settings)
        {
            lock (_lock)
            {
                using var transaction = _connection.BeginTransaction();
                try
                {
                    SaveSetting("MaxAttempts", settings.MaxAttempts.ToString());
                    SaveSetting("TimeWindow", settings.TimeWindow.ToString());
                    SaveSetting("PrivateIPBanHours", settings.PrivateIPBanHours.ToString());
                    SaveSetting("PublicIPBanDays", settings.PublicIPBanDays.ToString());
                    SaveSetting("GlobalBanlistEnabled", settings.GlobalBanlistEnabled.ToString());
                    SaveSetting("GitHub_AccessToken", settings.GitHub?.AccessToken ?? "");
                    SaveSetting("GitHub_RefreshInterval", (settings.GitHub?.RefreshInterval ?? 30).ToString());
                    SaveSetting("GitHub_EnableRateLimitProtection", (settings.GitHub?.EnableRateLimitProtection ?? true).ToString());

                    // Clear and re-add IP lists
                    using var clearWhite = _connection.CreateCommand();
                    clearWhite.CommandText = "DELETE FROM WhitelistedIPs";
                    clearWhite.ExecuteNonQuery();

                    foreach (var ip in settings.WhitelistedIPs)
                    {
                        AddWhitelistedIP(ip);
                    }

                    using var clearBlack = _connection.CreateCommand();
                    clearBlack.CommandText = "DELETE FROM BlacklistedIPs";
                    clearBlack.ExecuteNonQuery();

                    foreach (var ip in settings.BlacklistedIPs)
                    {
                        AddBlacklistedIP(ip);
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        #endregion

        #region Whitelisted/Blacklisted IPs

        private void AddWhitelistedIP(IPEntry ip)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO WhitelistedIPs (IPAddress, Type, AddedDate, IsEnabled, Notes, IsSubnet, PrefixLength, NetworkAddress, IsIPv6)
                VALUES (@ip, @type, @added, @enabled, @notes, @subnet, @prefix, @network, @ipv6)
            ";
            command.Parameters.AddWithValue("@ip", ip.IPAddress);
            command.Parameters.AddWithValue("@type", ip.Type);
            command.Parameters.AddWithValue("@added", ip.AddedDate.ToString("O"));
            command.Parameters.AddWithValue("@enabled", ip.IsEnabled ? 1 : 0);
            command.Parameters.AddWithValue("@notes", ip.Notes ?? "");
            command.Parameters.AddWithValue("@subnet", ip.IsSubnet ? 1 : 0);
            command.Parameters.AddWithValue("@prefix", ip.PrefixLength.HasValue ? ip.PrefixLength.Value : DBNull.Value);
            command.Parameters.AddWithValue("@network", ip.NetworkAddress ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ipv6", ip.IsIPv6 ? 1 : 0);
            command.ExecuteNonQuery();
        }

        private void AddBlacklistedIP(IPEntry ip)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO BlacklistedIPs (IPAddress, Type, AddedDate, IsEnabled, Notes, IsSubnet, PrefixLength, NetworkAddress, IsIPv6)
                VALUES (@ip, @type, @added, @enabled, @notes, @subnet, @prefix, @network, @ipv6)
            ";
            command.Parameters.AddWithValue("@ip", ip.IPAddress);
            command.Parameters.AddWithValue("@type", ip.Type);
            command.Parameters.AddWithValue("@added", ip.AddedDate.ToString("O"));
            command.Parameters.AddWithValue("@enabled", ip.IsEnabled ? 1 : 0);
            command.Parameters.AddWithValue("@notes", ip.Notes ?? "");
            command.Parameters.AddWithValue("@subnet", ip.IsSubnet ? 1 : 0);
            command.Parameters.AddWithValue("@prefix", ip.PrefixLength.HasValue ? ip.PrefixLength.Value : DBNull.Value);
            command.Parameters.AddWithValue("@network", ip.NetworkAddress ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ipv6", ip.IsIPv6 ? 1 : 0);
            command.ExecuteNonQuery();
        }

        private List<IPEntry> GetWhitelistedIPs()
        {
            var list = new List<IPEntry>();
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT * FROM WhitelistedIPs";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(ReadIPEntry(reader));
            }
            return list;
        }

        private List<IPEntry> GetBlacklistedIPs()
        {
            var list = new List<IPEntry>();
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT * FROM BlacklistedIPs";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(ReadIPEntry(reader));
            }
            return list;
        }

        private static IPEntry ReadIPEntry(SqliteDataReader reader)
        {
            return new IPEntry
            {
                IPAddress = reader.GetString(reader.GetOrdinal("IPAddress")),
                Type = reader.GetString(reader.GetOrdinal("Type")),
                AddedDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("AddedDate"))),
                IsEnabled = reader.GetInt32(reader.GetOrdinal("IsEnabled")) == 1,
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? "" : reader.GetString(reader.GetOrdinal("Notes")),
                IsSubnet = reader.GetInt32(reader.GetOrdinal("IsSubnet")) == 1,
                PrefixLength = reader.IsDBNull(reader.GetOrdinal("PrefixLength")) ? null : reader.GetInt32(reader.GetOrdinal("PrefixLength")),
                NetworkAddress = reader.IsDBNull(reader.GetOrdinal("NetworkAddress")) ? null : reader.GetString(reader.GetOrdinal("NetworkAddress")),
                IsIPv6 = reader.GetInt32(reader.GetOrdinal("IsIPv6")) == 1
            };
        }

        #endregion

        #region Banned IPs Operations

        public void SaveBannedIP(BanInfo ban)
        {
            lock (_lock)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = @"
                    INSERT OR REPLACE INTO BannedIPs (IPAddress, BanTime, Duration, ExpiryTime, AttemptCount, Location, Version)
                    VALUES (@ip, @banTime, @duration, @expiry, @attempts, @location, @version)
                ";
                command.Parameters.AddWithValue("@ip", ban.IPAddress);
                command.Parameters.AddWithValue("@banTime", ban.BanTime.ToString("O"));
                command.Parameters.AddWithValue("@duration", ban.Duration.ToString("c")); // Use invariant format
                command.Parameters.AddWithValue("@expiry", ban.ExpiryTime.ToString("O"));
                command.Parameters.AddWithValue("@attempts", ban.AttemptCount);
                command.Parameters.AddWithValue("@location", ban.Location);
                command.Parameters.AddWithValue("@version", (int)ban.Version);
                command.ExecuteNonQuery();
            }
        }

        public void SaveBannedIPs(Dictionary<string, BanInfo> bannedIPs)
        {
            lock (_lock)
            {
                using var transaction = _connection.BeginTransaction();
                try
                {
                    // Clear existing
                    using var clearCmd = _connection.CreateCommand();
                    clearCmd.CommandText = "DELETE FROM BannedIPs";
                    clearCmd.ExecuteNonQuery();

                    // Add all
                    foreach (var kvp in bannedIPs)
                    {
                        SaveBannedIP(kvp.Value);
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public Dictionary<string, BanInfo> LoadBannedIPs()
        {
            lock (_lock)
            {
                var result = new Dictionary<string, BanInfo>(StringComparer.OrdinalIgnoreCase);
                using var command = _connection.CreateCommand();
                command.CommandText = "SELECT * FROM BannedIPs";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var ban = new BanInfo
                    {
                        IPAddress = reader.GetString(reader.GetOrdinal("IPAddress")),
                        BanTime = DateTime.Parse(reader.GetString(reader.GetOrdinal("BanTime")), null, System.Globalization.DateTimeStyles.RoundtripKind),
                        Duration = TimeSpan.Parse(reader.GetString(reader.GetOrdinal("Duration")), System.Globalization.CultureInfo.InvariantCulture),
                        ExpiryTime = DateTime.Parse(reader.GetString(reader.GetOrdinal("ExpiryTime")), null, System.Globalization.DateTimeStyles.RoundtripKind),
                        AttemptCount = reader.GetInt32(reader.GetOrdinal("AttemptCount")),
                        Location = reader.GetString(reader.GetOrdinal("Location")),
                        Version = (IPValidator.IPVersion)reader.GetInt32(reader.GetOrdinal("Version"))
                    };
                    result[ban.IPAddress] = ban;
                }
                return result;
            }
        }

        public void RemoveBannedIP(string ipAddress)
        {
            lock (_lock)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = "DELETE FROM BannedIPs WHERE IPAddress = @ip";
                command.Parameters.AddWithValue("@ip", ipAddress);
                command.ExecuteNonQuery();
            }
        }

        #endregion

        #region Login Attempts Operations

        public void AddLoginAttempt(string ipAddress, DateTime timestamp)
        {
            lock (_lock)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO LoginAttempts (IPAddress, Timestamp)
                    VALUES (@ip, @timestamp)
                ";
                command.Parameters.AddWithValue("@ip", ipAddress);
                command.Parameters.AddWithValue("@timestamp", timestamp.ToString("O"));
                command.ExecuteNonQuery();
            }
        }

        public Dictionary<string, List<DateTime>> LoadLoginAttempts(TimeSpan maxAge)
        {
            lock (_lock)
            {
                var result = new Dictionary<string, List<DateTime>>(StringComparer.OrdinalIgnoreCase);
                var cutoff = DateTime.UtcNow.Subtract(maxAge);

                using var command = _connection.CreateCommand();
                command.CommandText = "SELECT IPAddress, Timestamp FROM LoginAttempts WHERE Timestamp > @cutoff ORDER BY Timestamp";
                command.Parameters.AddWithValue("@cutoff", cutoff.ToString("O"));

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var ip = reader.GetString(0);
                    var timestamp = DateTime.Parse(reader.GetString(1));

                    if (!result.ContainsKey(ip))
                    {
                        result[ip] = new List<DateTime>();
                    }
                    result[ip].Add(timestamp);
                }
                return result;
            }
        }

        public int GetRecentAttemptCount(string ipAddress, int timeWindowMinutes)
        {
            lock (_lock)
            {
                var cutoff = DateTime.UtcNow.AddMinutes(-timeWindowMinutes);
                using var command = _connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM LoginAttempts WHERE IPAddress = @ip AND Timestamp > @cutoff";
                command.Parameters.AddWithValue("@ip", ipAddress);
                command.Parameters.AddWithValue("@cutoff", cutoff.ToString("O"));
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        public int GetTotalAttempts(TimeSpan window)
        {
            lock (_lock)
            {
                var cutoff = DateTime.UtcNow.Subtract(window);
                using var command = _connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM LoginAttempts WHERE Timestamp > @cutoff";
                command.Parameters.AddWithValue("@cutoff", cutoff.ToString("O"));
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        public void RemoveLoginAttempts(string ipAddress)
        {
            lock (_lock)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = "DELETE FROM LoginAttempts WHERE IPAddress = @ip";
                command.Parameters.AddWithValue("@ip", ipAddress);
                command.ExecuteNonQuery();
            }
        }

        public void CleanupOldAttempts(TimeSpan maxAge)
        {
            lock (_lock)
            {
                var cutoff = DateTime.UtcNow.Subtract(maxAge);
                using var command = _connection.CreateCommand();
                command.CommandText = "DELETE FROM LoginAttempts WHERE Timestamp < @cutoff";
                command.Parameters.AddWithValue("@cutoff", cutoff.ToString("O"));
                command.ExecuteNonQuery();
            }
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _connection?.Close();
                _connection?.Dispose();
            }
        }
    }
}
