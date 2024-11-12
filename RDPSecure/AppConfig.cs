using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDPSecure
{
    public static class AppConfig
    {
        public static string AppDataPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "RDPSecure"
        );

        public static string LogsPath => Path.Combine(AppDataPath, "Logs");
        public static string SettingsPath => Path.Combine(AppDataPath, "settings.json");
        public static string AttemptsPath => Path.Combine(AppDataPath, "attempts.json"); 

        public static void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(AppDataPath);
            Directory.CreateDirectory(LogsPath);
        }
    }

}