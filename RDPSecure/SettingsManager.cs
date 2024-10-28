using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDPSecure
{
    public static class SettingsManager
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RDPSecure",
            "settings.json"
            );

        public static AppSettings LoadSettings()
        {
            try
            {
                EnsureSettingsFileExists(); // Add this line

                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading settings: {ex.Message}\nUsing default settings.",
                    "Settings Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }

            // If anything fails, return default settings
            var defaultSettings = new AppSettings();
            SaveSettings(defaultSettings); // Save default settings if none exist
            return defaultSettings;
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsPath)!;

                // Debug message
                MessageBox.Show($"Attempting to save to:\n{SettingsPath}", "Save Location");

                // Ensure directory exists
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    MessageBox.Show("Created directory", "Debug");
                }

                // Convert settings to JSON
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);

                // Debug - show what we're about to save
                MessageBox.Show($"About to save JSON:\n{json}", "Debug");

                // Save the file
                File.WriteAllText(SettingsPath, json);

                // Verify the save
                if (File.Exists(SettingsPath))
                {
                    string savedContent = File.ReadAllText(SettingsPath);
                    MessageBox.Show($"Verified saved content:\n{savedContent}", "Save Verification");
                }
                else
                {
                    throw new Exception("File was not created after save attempt");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Save Error");
                throw;
            }
        }

        private static void EnsureDirectoryExists()
        {
            string directory = Path.GetDirectoryName(SettingsPath)!;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static void EnsureSettingsFileExists()
        {
            if (!File.Exists(SettingsPath))
            {
                EnsureDirectoryExists();
                // Create default settings file if it doesn't exist
                SaveSettings(new AppSettings());
            }
        }

    }


}
