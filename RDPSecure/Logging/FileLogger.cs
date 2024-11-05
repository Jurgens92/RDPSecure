namespace RDPSecure.Logging
{
    public class FileLogger
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "RDPSecure",
            "Logs"
        );

        private static readonly string LogFile = Path.Combine(LogPath, "service.log");
        private const int MAX_LOG_SIZE = 10 * 1024 * 1024; // 10MB
        private const int MAX_LOG_FILES = 5;
        private static readonly object _lockObj = new object();

        public static void Initialize()
        {
            try
            {
                if (!Directory.Exists(LogPath))
                {
                    Directory.CreateDirectory(LogPath);
                }
            }
            catch (Exception ex)
            {
                // Write to temp folder if no access to ProgramData
                var tempPath = Path.Combine(Path.GetTempPath(), "RDPSecure", "Logs");
                Directory.CreateDirectory(tempPath);
                File.WriteAllText(
                    Path.Combine(tempPath, "init_error.log"),
                    $"{DateTime.Now}: Failed to create log directory: {ex.Message}"
                );
            }
        }

        public static void Log(string message)
        {
            try
            {
                lock (_lockObj)
                {
                    // Check if log rotation is needed
                    if (File.Exists(LogFile))
                    {
                        var fileInfo = new FileInfo(LogFile);
                        if (fileInfo.Length > MAX_LOG_SIZE)
                        {
                            RotateLogs();
                        }
                    }

                    var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
                    File.AppendAllText(LogFile, logMessage);
                }
            }
            catch (Exception ex)
            {
                // Try writing to temp folder if main log fails
                var tempLog = Path.Combine(Path.GetTempPath(), "RDPSecure", "service_fallback.log");
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(tempLog)!);
                    File.AppendAllText(tempLog,
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - ORIGINAL MESSAGE: {message}{Environment.NewLine}" +
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - LOG ERROR: {ex.Message}{Environment.NewLine}"
                    );
                }
                catch { }
            }
        }

        private static void RotateLogs()
        {
            try
            {
                // Remove the oldest log file if it exists
                string oldestLog = Path.Combine(LogPath, $"service.{MAX_LOG_FILES}.log");
                if (File.Exists(oldestLog))
                {
                    File.Delete(oldestLog);
                }

                // Shift all existing log files
                for (int i = MAX_LOG_FILES - 1; i >= 1; i--)
                {
                    string currentLog = Path.Combine(LogPath, $"service.{i}.log");
                    string nextLog = Path.Combine(LogPath, $"service.{i + 1}.log");
                    if (File.Exists(currentLog))
                    {
                        File.Move(currentLog, nextLog, true);
                    }
                }

                // Move current log file
                string backupLog = Path.Combine(LogPath, "service.1.log");
                if (File.Exists(LogFile))
                {
                    File.Move(LogFile, backupLog, true);
                }
            }
            catch (Exception ex)
            {
                // If rotation fails, try to write to temp log
                var tempLog = Path.Combine(Path.GetTempPath(), "RDPSecure", "service_rotation_error.log");
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(tempLog)!);
                    File.AppendAllText(tempLog,
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Error rotating logs: {ex.Message}{Environment.NewLine}"
                    );
                }
                catch { }
            }
        }
    }
}