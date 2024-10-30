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
                    int retryCount = 0;
                    const int maxRetries = 3;
                    const int delayMs = 500;

                    while (retryCount < maxRetries)
                    {
                        try
                        {
                            EnsureDirectoryExists();
                            string json = JsonConvert.SerializeObject(bannedIPs, Formatting.Indented);
                            File.WriteAllText(BannedIPsPath, json);
                            return; // Success, exit the method
                        }
                        catch (IOException ex) when (retryCount < maxRetries - 1)
                        {
                            retryCount++;
                            // Log the retry attempt
                            File.AppendAllText(
                                Path.Combine(AppDataPath, "error.log"),
                                $"{DateTime.Now}: Retry {retryCount}/{maxRetries} saving banned IPs. Error: {ex.Message}\n"
                            );
                            Thread.Sleep(delayMs); // Wait before retrying
                        }
                    }

                    // If we get here, all retries failed
                    throw new IOException($"Unable to save banned IPs after {maxRetries} attempts. File may be locked.");
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't show message box as this might be called from a background thread
                File.AppendAllText(
                    Path.Combine(AppDataPath, "error.log"),
                    $"{DateTime.Now}: Error saving banned IPs: {ex.Message}\n"
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

    // Class to store ban information
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