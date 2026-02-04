// UpdateManager.cs
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using RDPSecure.Logging;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text.RegularExpressions;

namespace RDPSecure
{
    public class UpdateManager
    {
        // Static HttpClient to avoid socket exhaustion
        private static readonly HttpClient _httpClient;

        static UpdateManager()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
            // GitHub API requires a User-Agent header
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("RDPSecure", Program.VERSION));
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        }

        private readonly string _owner;
        private readonly string _repo;
        private readonly ISecurityLogger _logger;
        private readonly string _currentVersion;

        /// <summary>
        /// Creates an UpdateManager that checks GitHub Releases for updates
        /// </summary>
        /// <param name="owner">GitHub repository owner (e.g., "Jurgens92")</param>
        /// <param name="repo">GitHub repository name (e.g., "RDPSecure")</param>
        /// <param name="currentVersion">Current application version</param>
        /// <param name="logger">Logger instance</param>
        public UpdateManager(string owner, string repo, string currentVersion, ISecurityLogger logger)
        {
            _owner = owner;
            _repo = repo;
            _logger = logger;
            _currentVersion = currentVersion;
        }

        /// <summary>
        /// Checks GitHub Releases for a newer version
        /// </summary>
        public async Task<UpdateInfo?> CheckForUpdates()
        {
            try
            {
                var apiUrl = $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest";
                _logger.LogInformation($"Checking for updates at: {apiUrl}");

                var response = await _httpClient.GetAsync(apiUrl);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("No releases found");
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var release = JObject.Parse(json);

                // Get version from tag (remove 'v' prefix if present)
                var tagName = release["tag_name"]?.ToString() ?? "";
                var version = tagName.TrimStart('v', 'V');

                if (string.IsNullOrEmpty(version))
                {
                    _logger.LogError("Release has no version tag");
                    return null;
                }

                if (!IsNewVersionAvailable(version))
                {
                    _logger.LogInformation($"Current version {_currentVersion} is up to date (latest: {version})");
                    return null;
                }

                // Find the zip asset
                var assets = release["assets"] as JArray;
                var zipAsset = assets?.FirstOrDefault(a =>
                    a["name"]?.ToString().EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true);

                if (zipAsset == null)
                {
                    _logger.LogError("No zip asset found in release");
                    return null;
                }

                var downloadUrl = zipAsset["browser_download_url"]?.ToString() ?? "";
                var releaseNotes = release["body"]?.ToString() ?? "";
                var releaseDate = release["published_at"]?.ToObject<DateTime>() ?? DateTime.Now;
                var isPrerelease = release["prerelease"]?.ToObject<bool>() ?? false;

                // Try to extract checksum from release notes (format: SHA256: <hash>)
                var checksum = ExtractChecksumFromNotes(releaseNotes);

                _logger.LogInformation($"New version available: {version}");

                return new UpdateInfo
                {
                    Version = version,
                    DownloadUrl = downloadUrl,
                    ReleaseNotes = releaseNotes,
                    Checksum = checksum,
                    ReleaseDate = releaseDate,
                    IsPrerelease = isPrerelease,
                    TagName = tagName,
                    AssetName = zipAsset["name"]?.ToString() ?? "",
                    AssetSize = zipAsset["size"]?.ToObject<long>() ?? 0
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Network error checking for updates: {ex.Message}");
                return null; // Don't throw - network errors are expected sometimes
            }
            catch (Exception ex)
            {
                _logger.LogError("Error checking for updates", ex);
                throw;
            }
        }

        /// <summary>
        /// Extracts SHA256 checksum from release notes if present
        /// Looks for patterns like "SHA256: abc123..." or "Checksum: abc123..."
        /// </summary>
        private string ExtractChecksumFromNotes(string releaseNotes)
        {
            if (string.IsNullOrEmpty(releaseNotes))
                return string.Empty;

            // Match patterns like "SHA256: <64 hex chars>" or "Checksum: <64 hex chars>"
            var match = Regex.Match(releaseNotes,
                @"(?:SHA256|Checksum):\s*([a-fA-F0-9]{64})",
                RegexOptions.IgnoreCase);

            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private bool IsNewVersionAvailable(string newVersion)
        {
            try
            {
                // Handle versions like "1.0.0" or "1.0.0.0"
                var current = ParseVersion(_currentVersion);
                var available = ParseVersion(newVersion);
                return available > current;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error comparing versions '{_currentVersion}' vs '{newVersion}': {ex.Message}");
                return false;
            }
        }

        private Version ParseVersion(string versionString)
        {
            // Remove any 'v' prefix and trim
            versionString = versionString.TrimStart('v', 'V').Trim();

            // Ensure we have at least major.minor format
            var parts = versionString.Split('.');
            if (parts.Length < 2)
            {
                versionString += ".0";
            }

            return Version.Parse(versionString);
        }

        public async Task<bool> DownloadAndInstallUpdate(UpdateInfo updateInfo, IProgress<int>? progress = null)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "RDPSecureUpdate");
            string updateZip = Path.Combine(tempPath, "update.zip");
            string extractPath = Path.Combine(tempPath, "extracted");
            string installPath = Path.GetDirectoryName(Application.ExecutablePath)!;

            try
            {
                _logger.LogInformation($"Starting update to version {updateInfo.Version}");
                _logger.LogInformation($"Install path: {installPath}");

                // Clean up any previous update attempt
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }

                Directory.CreateDirectory(tempPath);
                Directory.CreateDirectory(extractPath);

                // Download update
                _logger.LogInformation($"Downloading from: {updateInfo.DownloadUrl}");
                await DownloadUpdate(updateInfo.DownloadUrl, updateZip, progress);

                // Verify checksum if provided
                if (!string.IsNullOrEmpty(updateInfo.Checksum))
                {
                    _logger.LogInformation("Verifying checksum...");
                    var isValid = VerifyChecksum(updateZip, updateInfo.Checksum);
                    if (!isValid)
                    {
                        _logger.LogError("Update package checksum verification failed");
                        throw new Exception("Update package verification failed. The download may be corrupted.");
                    }
                    _logger.LogInformation("Checksum verified successfully");
                }

                // Extract update
                _logger.LogInformation("Extracting update...");
                ZipFile.ExtractToDirectory(updateZip, extractPath, true);

                // Stop the service/application
                await StopApplication();

                // Backup current version
                string backupPath = Path.Combine(AppConfig.AppDataPath, "backups");
                Directory.CreateDirectory(backupPath);
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string backupZip = Path.Combine(backupPath, $"backup_v{_currentVersion}_{timestamp}.zip");

                _logger.LogInformation($"Creating backup at: {backupZip}");
                ZipFile.CreateFromDirectory(Application.StartupPath, backupZip);

                // Copy new files
                _logger.LogInformation("Installing new files...");
                foreach (var file in Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var relativePath = Path.GetRelativePath(extractPath, file);
                        var targetPath = Path.Combine(installPath, relativePath);

                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

                        if (File.Exists(targetPath))
                        {
                            var tempFilePath = targetPath + ".new";
                            File.Copy(file, tempFilePath, true);
                            File.Replace(tempFilePath, targetPath, targetPath + ".old");
                            // Clean up .old file
                            try { File.Delete(targetPath + ".old"); } catch { }
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

                // Cleanup temp files
                try
                {
                    Directory.Delete(tempPath, true);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error cleaning up temp files: {ex.Message}");
                }

                // Cleanup old backups (keep last 3)
                CleanupOldBackups(backupPath, 3);

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

        private void CleanupOldBackups(string backupPath, int keepCount)
        {
            try
            {
                var backups = Directory.GetFiles(backupPath, "backup_*.zip")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .Skip(keepCount)
                    .ToList();

                foreach (var backup in backups)
                {
                    try
                    {
                        File.Delete(backup);
                        _logger.LogInformation($"Deleted old backup: {Path.GetFileName(backup)}");
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cleaning up old backups: {ex.Message}");
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

                var buffer = new byte[81920]; // 80KB buffer for faster downloads
                var totalBytesRead = 0L;
                var bytesRead = 0;

                while ((bytesRead = await contentStream.ReadAsync(buffer)) != 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    totalBytesRead += bytesRead;

                    if (progress != null && totalBytes > 0)
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
                var expected = expectedChecksum.ToLowerInvariant();

                var isValid = string.Equals(actualChecksum, expected, StringComparison.OrdinalIgnoreCase);

                if (!isValid)
                {
                    _logger.LogError($"Checksum mismatch. Expected: {expected}, Got: {actualChecksum}");
                }

                return isValid;
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
                    _logger.LogInformation("Stopping RDPSecure service...");
                    using var service = new ServiceController("RDPSecure");
                    if (service.Status == ServiceControllerStatus.Running)
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                        _logger.LogInformation("Service stopped");
                    }
                }

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

                var startInfo = new ProcessStartInfo
                {
                    FileName = appPath,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(startInfo);
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
        public DateTime ReleaseDate { get; set; }
        public bool IsPrerelease { get; set; }
        public string TagName { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public long AssetSize { get; set; }

        // Keep for backwards compatibility
        public bool IsCritical { get; set; }
    }
}
