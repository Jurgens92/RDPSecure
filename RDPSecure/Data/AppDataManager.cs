using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDPSecure.Data;

    public static class AppDataManager
    {
        // Base directory for all application data
        public static readonly string BaseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "RDPSecure"
        );

        // Specific directories
        public static readonly string ConfigDirectory = Path.Combine(BaseDirectory, "Config");
        public static readonly string LogsDirectory = Path.Combine(BaseDirectory, "Logs");
        public static readonly string DatabaseDirectory = Path.Combine(BaseDirectory, "Data");

        // File paths
        public static readonly string SettingsFile = Path.Combine(ConfigDirectory, "settings.json");
        public static readonly string BannedIPsFile = Path.Combine(DatabaseDirectory, "banned_ips.json");
        public static readonly string WhitelistedIPsFile = Path.Combine(DatabaseDirectory, "whitelist.json");
        public static readonly string LogFile = Path.Combine(LogsDirectory, "rdpsecure.log");

        static AppDataManager()
        {
            // Create directories if they don't exist
            CreateDirectories();
        }

        private static void CreateDirectories()
        {
            Directory.CreateDirectory(ConfigDirectory);
            Directory.CreateDirectory(LogsDirectory);
            Directory.CreateDirectory(DatabaseDirectory);
        }
    }


