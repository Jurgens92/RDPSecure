using RDPSecure.Logging;
using RDPSecure.Services;
using System.ServiceProcess;
using System.Diagnostics;

namespace RDPSecure
{
    public partial class RDPSecureService : ServiceBase
    {
        public RDPSecureService()
        {
            InitializeComponent();
            ServiceName = "RDPSecure";
        }

        protected override void OnStart(string[] args)
        {
            WriteToFile("Service is starting");

            try
            {
                WriteToFile("Loading settings...");
                var settings = SettingsManager.LoadSettings();

                WriteToFile("Creating monitor service...");
                var monitorService = new RDPMonitorService(settings);

                WriteToFile("Starting monitoring...");
                monitorService.StartMonitoring();

                WriteToFile("Service started successfully");
            }
            catch (Exception ex)
            {
                WriteToFile($"Error starting service: {ex}");
                throw;
            }
        }

        protected override void OnStop()
        {
            WriteToFile("Service is stopping");
        }

        private void WriteToFile(string message)
        {
            var locations = new[]
            {
                @"C:\RDPSecure",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RDPSecure"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RDPSecure"),
                Path.GetTempPath()
            };

            foreach (var baseLocation in locations)
            {
                try
                {
                    var logPath = Path.Combine(baseLocation, "service.log");
                    Directory.CreateDirectory(baseLocation);
                    File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
                    break;  // If successful, exit the loop
                }
                catch
                {
                    continue;  // Try next location
                }
            }
        }
    }
}