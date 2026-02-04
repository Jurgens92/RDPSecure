using RDPSecure.Logging;
using RDPSecure.Services;
using System.ServiceProcess;
using System.Diagnostics;

namespace RDPSecure
{
    public partial class RDPSecureService : ServiceBase
    {
        private RDPMonitorService? _monitorService;
        private ISecurityLogger? _logger;
        private readonly System.Timers.Timer _stateCheckTimer;
        private DateTime _lastStateLogTime = DateTime.MinValue;
        private const int STATE_LOG_INTERVAL_MINUTES = 60; // Log state once per hour
        public RDPSecureService()
        {
            InitializeComponent();
            ServiceName = "RDPSecure";

            // Create timer to periodically check state
            _stateCheckTimer = new System.Timers.Timer(30000); // Check every 30 seconds
            _stateCheckTimer.Elapsed += OnStateCheck;
        }

        private void CleanupResources()
        {
            try
            {
                if (_stateCheckTimer != null)
                {
                    _stateCheckTimer.Stop();
                    _stateCheckTimer.Elapsed -= OnStateCheck;  // Remove event handler
                    _stateCheckTimer.Dispose();
                }

                if (_monitorService != null)
                {
                    _monitorService.StopMonitoring();
                    _monitorService.Dispose();
                }
            }
            catch (Exception ex)
            {
                WriteToFile($"Error during cleanup: {ex.Message}");
            }
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                WriteToFile("Service is starting");

                // Ensure we have proper permissions to the directories
                AppConfig.EnsureDirectoriesExist();

                // Set permissions if needed
                EnsureDirectoryPermissions(AppConfig.AppDataPath);

                _logger = new SecurityLogger();
                WriteToFile("Loading settings...");
                var settings = SettingsManager.LoadSettings();

                WriteToFile("Creating monitor service...");
                _monitorService = new RDPMonitorService(settings);

                WriteToFile("Starting monitoring...");
                _monitorService.StartMonitoring();

                WriteToFile("Service started successfully");
            }
            catch (Exception ex)
            {
                WriteToFile($"Error starting service: {ex}");
                throw;
            }
        }

        private void EnsureDirectoryPermissions(string path)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(path);
                var directorySecurity = directoryInfo.GetAccessControl();

                // Add full control for SYSTEM (required for service operation)
                var systemSid = new System.Security.Principal.SecurityIdentifier(
                    System.Security.Principal.WellKnownSidType.LocalSystemSid, null);
                directorySecurity.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                    systemSid,
                    System.Security.AccessControl.FileSystemRights.FullControl,
                    System.Security.AccessControl.InheritanceFlags.ContainerInherit |
                    System.Security.AccessControl.InheritanceFlags.ObjectInherit,
                    System.Security.AccessControl.PropagationFlags.None,
                    System.Security.AccessControl.AccessControlType.Allow));

                // Add full control for Administrators
                var adminsSid = new System.Security.Principal.SecurityIdentifier(
                    System.Security.Principal.WellKnownSidType.BuiltinAdministratorsSid, null);
                directorySecurity.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                    adminsSid,
                    System.Security.AccessControl.FileSystemRights.FullControl,
                    System.Security.AccessControl.InheritanceFlags.ContainerInherit |
                    System.Security.AccessControl.InheritanceFlags.ObjectInherit,
                    System.Security.AccessControl.PropagationFlags.None,
                    System.Security.AccessControl.AccessControlType.Allow));

                // Add read/write for authenticated users (allows GUI app to work when run as admin)
                var authenticatedUsersSid = new System.Security.Principal.SecurityIdentifier(
                    System.Security.Principal.WellKnownSidType.AuthenticatedUserSid, null);
                directorySecurity.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                    authenticatedUsersSid,
                    System.Security.AccessControl.FileSystemRights.Read |
                    System.Security.AccessControl.FileSystemRights.Write |
                    System.Security.AccessControl.FileSystemRights.CreateFiles |
                    System.Security.AccessControl.FileSystemRights.CreateDirectories |
                    System.Security.AccessControl.FileSystemRights.Delete,
                    System.Security.AccessControl.InheritanceFlags.ContainerInherit |
                    System.Security.AccessControl.InheritanceFlags.ObjectInherit,
                    System.Security.AccessControl.PropagationFlags.None,
                    System.Security.AccessControl.AccessControlType.Allow));

                directoryInfo.SetAccessControl(directorySecurity);
            }
            catch (Exception ex)
            {
                WriteToFile($"Error setting directory permissions: {ex.Message}");
            }
        }

        private void OnStateCheck(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (_monitorService != null)
                {
                    // Reload bans from storage
                    var savedBans = SettingsManager.LoadBannedIPs();
                    var settings = SettingsManager.LoadSettings();

                    // Log current state only once per hour
                    if (DateTime.Now - _lastStateLogTime > TimeSpan.FromMinutes(STATE_LOG_INTERVAL_MINUTES))
                    {
                        WriteToFile($"State check - Active bans: {savedBans.Count}, Max attempts: {settings.MaxAttempts}");
                        _lastStateLogTime = DateTime.Now;
                    }
                    
                    // Update service state
                    _monitorService.OnSettingsChanged();
                    _monitorService.CleanupBannedIPs();
                }
            }
            catch (Exception ex)
            {
                WriteToFile($"Error during state check: {ex.Message}");
            }
        }

        protected override void OnStop()
        {
            try
            {
                WriteToFile("Service is stopping");
                CleanupResources();
                WriteToFile("Service stopped successfully");
            }
            catch (Exception ex)
            {
                WriteToFile($"Error stopping service: {ex}");
            }
        }

        protected override void OnShutdown()
        {
            try
            {
                WriteToFile("Service is shutting down");
                CleanupResources();
                WriteToFile("Service shutdown completed");
            }
            catch (Exception ex)
            {
                WriteToFile($"Error during shutdown: {ex}");
            }
            base.OnShutdown();
        }

        private void WriteToFile(string message)
        {
            try
            {
                string baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "RDPSecure"
                );
                Directory.CreateDirectory(baseDir);
                string logPath = Path.Combine(baseDir, "service.log");
                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
            }
            catch
            {
                try
                {
                    string tempPath = Path.Combine(Path.GetTempPath(), "RDPSecure_service.log");
                    File.AppendAllText(tempPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
                }
                catch {}
            }
        }
    }
}