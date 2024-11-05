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
            WriteToFile("Service is starting");

            try
            {
                _logger = new SecurityLogger();
                WriteToFile("Loading settings...");
                var settings = SettingsManager.LoadSettings();

                WriteToFile("Creating monitor service...");
                _monitorService = new RDPMonitorService(settings);

                WriteToFile("Starting monitoring...");
                _monitorService.StartMonitoring();
                _stateCheckTimer.Start();

                WriteToFile("Service started successfully");
            }
            catch (Exception ex)
            {
                WriteToFile($"Error starting service: {ex}");
                throw;
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