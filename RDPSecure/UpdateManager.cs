// UpdateManager.cs
using System.Net.Http;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.IO.Compression;
using RDPSecure.Logging;
using System.Diagnostics;
using System.ServiceProcess;

namespace RDPSecure
{
    public class UpdateManager
    {
        private readonly string _updateServerUrl;
        private readonly HttpClient _httpClient;
        private readonly ISecurityLogger _logger;
        private readonly string _currentVersion;

        public UpdateManager(string updateServerUrl, string currentVersion, ISecurityLogger logger)
        {
            _updateServerUrl = updateServerUrl.TrimEnd('/');
            _httpClient = new HttpClient();
            _logger = logger;
            _currentVersion = currentVersion;
        }

        public async Task<UpdateInfo?> CheckForUpdates()
        {
            try
            {
                var versionUrl = $"{_updateServerUrl}/version.json";
                var response = await _httpClient.GetStringAsync(versionUrl);
                var updateInfo = JsonConvert.DeserializeObject<UpdateInfo>(response);

                if (updateInfo != null && IsNewVersionAvailable(updateInfo.Version))
                {
                    _logger.LogInformation($"New version available: {updateInfo.Version}");
                    return updateInfo;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error checking for updates", ex);
                throw;
            }
        }

        private bool IsNewVersionAvailable(string newVersion)
        {
            try
            {
                var current = Version.Parse(_currentVersion);
                var available = Version.Parse(newVersion);
                return available > current;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error comparing versions", ex);
                return false;
            }
        }

        public async Task<bool> DownloadAndInstallUpdate(UpdateInfo updateInfo, IProgress<int>? progress = null)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "RDPSecureUpdate");
            string updateZip = Path.Combine(tempPath, "update.zip");
            string extractPath = Path.Combine(tempPath, "extracted");
            string installPath = Path.GetDirectoryName(Application.ExecutablePath)!;

            try
            {
                _logger.LogInformation($"Starting update installation to: {installPath}");
                // Create temp directory
                Directory.CreateDirectory(tempPath);
                Directory.CreateDirectory(extractPath);

                // Download update
                await DownloadUpdate(updateInfo.DownloadUrl, updateZip, progress);

                // Verify checksum if provided
                if (!string.IsNullOrEmpty(updateInfo.Checksum))
                {
                    var isValid = VerifyChecksum(updateZip, updateInfo.Checksum);
                    if (!isValid)
                    {
                        _logger.LogError("Update package checksum verification failed");
                        throw new Exception("Update package verification failed");
                    }
                }

                // Extract update
                ZipFile.ExtractToDirectory(updateZip, extractPath, true);

                // Stop the service/application
                await StopApplication();

                // Backup current version
                string backupPath = Path.Combine(AppConfig.AppDataPath, "backup");
                Directory.CreateDirectory(backupPath);
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string backupZip = Path.Combine(backupPath, $"backup_{timestamp}.zip");
                ZipFile.CreateFromDirectory(Application.StartupPath, backupZip);

                // Copy new files
                foreach (var file in Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var relativePath = Path.GetRelativePath(extractPath, file);
                        var targetPath = Path.Combine(installPath, relativePath);

                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

                        // Handle file in use
                        if (File.Exists(targetPath))
                        {
                            var tempFilePath = targetPath + ".temp";  // Changed from tempPath to tempFilePath
                            File.Copy(file, tempFilePath, true);
                            File.Replace(tempFilePath, targetPath, targetPath + ".backup");
                        }
                        else
                        {
                            File.Copy(file, targetPath, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error copying file {file}: {ex.Message}");
                        throw;
                    }
                }

                _logger.LogInformation($"Update to version {updateInfo.Version} installed successfully");

                // Cleanup
                try
                {
                    Directory.Delete(tempPath, true);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error cleaning up temp files: {ex.Message}");
                    // Continue anyway as this is not critical
                }

                // Restart application
                RestartApplication();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error installing update", ex);
                throw;
            }
        }

        private async Task DownloadUpdate(string downloadUrl, string savePath, IProgress<int>? progress)
        {
            try
            {
                using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);

                var buffer = new byte[8192];
                var totalBytesRead = 0L;
                var bytesRead = 0;

                while ((bytesRead = await contentStream.ReadAsync(buffer)) != 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    totalBytesRead += bytesRead;

                    if (progress != null && totalBytes != -1)
                    {
                        var progressPercentage = (int)((totalBytesRead * 100) / totalBytes);
                        progress.Report(progressPercentage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading update from {downloadUrl}", ex);
                throw;
            }
        }

        private bool VerifyChecksum(string filePath, string expectedChecksum)
        {
            try
            {
                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                var hash = sha256.ComputeHash(stream);
                var actualChecksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                return string.Equals(actualChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error verifying update checksum", ex);
                return false;
            }
        }

        private async Task StopApplication()
        {
            try
            {
                // Stop the Windows service if running
                if (ServiceExists("RDPSecure"))
                {
                    using var service = new ServiceController("RDPSecure");
                    if (service.Status == ServiceControllerStatus.Running)
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    }
                }

                // Allow time for processes to stop
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error stopping application", ex);
                throw;
            }
        }

        private bool ServiceExists(string serviceName)
        {
            return ServiceController.GetServices().Any(s => s.ServiceName == serviceName);
        }

        private void RestartApplication()
        {
            try
            {
                string appPath = Application.ExecutablePath;
                _logger.LogInformation($"Restarting application from: {appPath}");

                // Start a new process to restart the application
                var startInfo = new ProcessStartInfo
                {
                    FileName = appPath,
                    UseShellExecute = true,
                    Verb = "runas" // Request admin privileges
                };

                Process.Start(startInfo);

                // Exit current instance
                Application.Exit();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error restarting application", ex);
                throw;
            }
        }
    }

    public class UpdateInfo
    {
        public string Version { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public string Checksum { get; set; } = string.Empty;
        public bool IsCritical { get; set; }
        public DateTime ReleaseDate { get; set; }
    }
}