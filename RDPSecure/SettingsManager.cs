using RDPSecure.Data;

namespace RDPSecure
{
    public static class SettingsManager
    {
        private static readonly Lazy<DatabaseManager> _db = new(() => new DatabaseManager());
        private static DatabaseManager Database => _db.Value;

        public static AppSettings LoadSettings()
        {
            try
            {
                return Database.LoadSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading settings: {ex.Message}\nUsing default settings.",
                    "Settings Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return new AppSettings();
            }
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                Database.SaveSettings(settings);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error saving settings: {ex.Message}",
                    "Settings Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        public static void SaveBannedIPs(Dictionary<string, BanInfo> bannedIPs)
        {
            try
            {
                Database.SaveBannedIPs(bannedIPs);
            }
            catch (Exception ex)
            {
                // Log the error but don't show message box as this might be called from a background thread
                var logPath = Path.Combine(AppConfig.AppDataPath, "error.log");
                File.AppendAllText(logPath, $"{DateTime.Now}: Error saving banned IPs: {ex.Message}\n");
            }
        }

        public static Dictionary<string, BanInfo> LoadBannedIPs()
        {
            try
            {
                return Database.LoadBannedIPs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading banned IPs: {ex.Message}",
                    "Load Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return new Dictionary<string, BanInfo>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    // Class to store ban information
    public class BanInfo
    {
        public string IPAddress { get; set; } = string.Empty;
        public DateTime BanTime { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime ExpiryTime { get; set; }
        public int AttemptCount { get; set; }
        public string Location { get; set; } = "Detecting...";
        public IPValidator.IPVersion Version { get; set; }

        // Helper property to determine if this is an IPv6 address
        public bool IsIPv6 => Version == IPValidator.IPVersion.IPv6;

        // Normalize the IP address when setting it
        public void SetIPAddress(string ip)
        {
            IPAddress = IPValidator.NormalizeIP(ip);
            var (_, _, version) = IPValidator.ValidateIP(ip);
            Version = version;
        }
    }
}
