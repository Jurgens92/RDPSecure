using System;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace RDPSecure.Logging
{
    public class SecurityLogger : ISecurityLogger
    {
        private string _logPath;
        private readonly object _lockObj = new object();
        private const int MAX_LOG_SIZE = 10 * 1024 * 1024; // 10MB
        private const int MAX_LOG_FILES = 5;

        public SecurityLogger()
        {
            _logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "RDPSecure",
                "Logs"
            );

            EnsureLogDirectoryExists();
        }

        private void EnsureLogDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_logPath))
                {
                    Directory.CreateDirectory(_logPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create log directory: {ex.Message}");
                // Fall back to temp directory if can't access ProgramData
                _logPath = Path.Combine(Path.GetTempPath(), "RDPSecure", "Logs");
                Directory.CreateDirectory(_logPath);
            }
        }

        public void LogInformation(string message)
        {
            WriteToFile("INFO", message);
            Debug.WriteLine($"INFO: {message}");
        }

        public void LogWarning(string message)
        {
            WriteToFile("WARNING", message);
            Debug.WriteLine($"WARNING: {message}");
        }

        public void LogError(string message, Exception? ex = null)
        {
            var sb = new StringBuilder();
            sb.Append(message);
            if (ex != null)
            {
                sb.AppendLine();
                sb.Append("Exception: ").Append(ex.Message);
                sb.AppendLine();
                sb.Append("Stack Trace: ").Append(ex.StackTrace);
            }

            WriteToFile("ERROR", sb.ToString());
            Debug.WriteLine($"ERROR: {sb}");
        }

        public void LogSecurityEvent(string ipAddress, string eventType, string details)
        {
            WriteToFile("SECURITY", $"IP={ipAddress}, Event={eventType}, Details={details}");
            Debug.WriteLine($"SECURITY: IP={ipAddress}, Event={eventType}, Details={details}");
        }

        private void WriteToFile(string level, string message)
        {
            try
            {
                var logFile = Path.Combine(_logPath, "rdpsecure.log");
                var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}";

                lock (_lockObj)
                {
                    // Check if log rotation is needed
                    if (File.Exists(logFile) && new FileInfo(logFile).Length > MAX_LOG_SIZE)
                    {
                        RotateLogs();
                    }

                    File.AppendAllText(logFile, logMessage);
                }
            }
            catch (Exception ex)
            {
                // If can't write to main log, try writing to temp directory
                try
                {
                    var tempLog = Path.Combine(Path.GetTempPath(), "RDPSecure", "fallback.log");
                    Directory.CreateDirectory(Path.GetDirectoryName(tempLog)!);
                    File.AppendAllText(tempLog,
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ERROR] Failed to write to main log: {ex.Message}{Environment.NewLine}");
                }
                catch
                {
                    // Write to debug output
                    Debug.WriteLine($"CRITICAL: Failed to write to any log file: {ex.Message}");
                }
            }
        }

        private void RotateLogs()
        {
            try
            {
                // Remove the oldest log file if it exists
                var oldestLog = Path.Combine(_logPath, $"rdpsecure.{MAX_LOG_FILES}.log");
                if (File.Exists(oldestLog))
                {
                    File.Delete(oldestLog);
                }

                // Rotate existing log files
                for (int i = MAX_LOG_FILES - 1; i >= 1; i--)
                {
                    var currentLog = Path.Combine(_logPath, $"rdpsecure.{i}.log");
                    var nextLog = Path.Combine(_logPath, $"rdpsecure.{i + 1}.log");
                    if (File.Exists(currentLog))
                    {
                        File.Move(currentLog, nextLog);
                    }
                }

                // Rotate the current log file
                var mainLog = Path.Combine(_logPath, "rdpsecure.log");
                var firstBackup = Path.Combine(_logPath, "rdpsecure.1.log");
                if (File.Exists(mainLog))
                {
                    File.Move(mainLog, firstBackup);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error rotating log files: {ex.Message}");
            }
        }
    }
}