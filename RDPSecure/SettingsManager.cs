using RDPSecure.Data;
using RDPSecure.Models;

namespace RDPSecure
{
    /// <summary>
    /// Manages application settings persistence.
    /// This class should not contain any UI logic - exceptions are thrown for the UI layer to handle.
    /// </summary>
    public static class SettingsManager
    {
        private static DatabaseManager Database => DatabaseProvider.Instance;

        /// <summary>
        /// Loads application settings from the database.
        /// Returns default settings if loading fails.
        /// </summary>
        /// <returns>Application settings</returns>
        public static AppSettings LoadSettings()
        {
            try
            {
                return Database.LoadSettings();
            }
            catch (Exception)
            {
                // Return default settings if loading fails
                // The caller can decide how to notify the user
                return new AppSettings();
            }
        }

        /// <summary>
        /// Saves application settings to the database.
        /// </summary>
        /// <param name="settings">Settings to save</param>
        /// <exception cref="SettingsException">Thrown when saving fails</exception>
        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                Database.SaveSettings(settings);
            }
            catch (Exception ex)
            {
                throw new SettingsException($"Failed to save settings: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Saves banned IPs to the database.
        /// Errors are logged but not thrown to avoid disrupting background operations.
        /// </summary>
        public static void SaveBannedIPs(Dictionary<string, BanInfo> bannedIPs)
        {
            try
            {
                Database.SaveBannedIPs(bannedIPs);
            }
            catch (Exception ex)
            {
                // Log the error but don't throw as this might be called from a background thread
                var logPath = Path.Combine(AppConfig.AppDataPath, "error.log");
                try
                {
                    File.AppendAllText(logPath, $"{DateTime.Now}: Error saving banned IPs: {ex.Message}\n");
                }
                catch
                {
                    // Ignore logging failures
                }
            }
        }

        /// <summary>
        /// Loads banned IPs from the database.
        /// Returns an empty dictionary if loading fails.
        /// </summary>
        public static Dictionary<string, BanInfo> LoadBannedIPs()
        {
            try
            {
                return Database.LoadBannedIPs();
            }
            catch (Exception)
            {
                // Return empty dictionary if loading fails
                return new Dictionary<string, BanInfo>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>
    /// Exception thrown when settings operations fail.
    /// </summary>
    public class SettingsException : Exception
    {
        public SettingsException(string message) : base(message) { }
        public SettingsException(string message, Exception innerException) : base(message, innerException) { }
    }
}
