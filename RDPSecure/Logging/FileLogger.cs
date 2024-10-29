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
                // Write to temp folder if we can't access ProgramData
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
                var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
                File.AppendAllText(LogFile, logMessage);
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
                catch
                {
                    // At this point we can't do anything else
                }
            }
        }
    }
}