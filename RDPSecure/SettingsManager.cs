using Newtonsoft.Json;
using System;
using System.IO;

namespace RDPSecure
{
    public static class SettingsManager
    {
        private static readonly string AppDataPath = Path.Combine(
       Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
       "RDPSecure"
       );

        private static readonly string SettingsPath = Path.Combine(AppDataPath, "settings.json");
        private static readonly string BannedIPsPath = Path.Combine(AppDataPath, "banned_ips.json");
        private static readonly object _fileLock = new object();


        public static AppSettings LoadSettings()
        {
            try
            {
                EnsureDirectoryExists();

                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }

                // If file doesn't exist or is invalid, create default settings
                var defaultSettings = new AppSettings();
                SaveSettings(defaultSettings);
                return defaultSettings;
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
                lock (_fileLock)
                {
                    EnsureDirectoryExists();
                    string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                    File.WriteAllText(SettingsPath, json);
                }
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

        // Add methods for banned IPs persistence
        public static void SaveBannedIPs(Dictionary<string, BanInfo> bannedIPs)
        {
            try
            {
                lock (_fileLock)
                {
                    EnsureDirectoryExists();
                    string json = JsonConvert.SerializeObject(bannedIPs, Formatting.Indented);
                    File.WriteAllText(BannedIPsPath, json);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error saving banned IPs: {ex.Message}",
                    "Save Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        public static Dictionary<string, BanInfo> LoadBannedIPs()
        {
            try
            {
                lock (_fileLock)
                {
                    if (File.Exists(BannedIPsPath))
                    {
                        string json = File.ReadAllText(BannedIPsPath);
                        var bannedIPs = JsonConvert.DeserializeObject<Dictionary<string, BanInfo>>(json);
                        return bannedIPs ?? new Dictionary<string, BanInfo>(StringComparer.OrdinalIgnoreCase);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading banned IPs: {ex.Message}",
                    "Load Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
            return new Dictionary<string, BanInfo>(StringComparer.OrdinalIgnoreCase);
        }

        private static void EnsureDirectoryExists()
        {
            if (!Directory.Exists(AppDataPath))
            {
                Directory.CreateDirectory(AppDataPath);
            }
        }
    }

    // Add this class to store ban information
    public class BanInfo
    {
        public string IPAddress { get; set; } = string.Empty;
        public DateTime BanTime { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime ExpiryTime { get; set; }
        public int AttemptCount { get; set; }
        public string Location { get; set; } = "Detecting...";
    }
}