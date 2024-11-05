using Microsoft.Extensions.DependencyInjection;
using RDPSecure.Services;
using RDPSecure.Logging;
using System.ServiceProcess;
using System.Threading;

namespace RDPSecure;

public static class Program
{
    public const string VERSION = "1.0.1";
    private static Mutex? _mutex;
    [STAThread]
    static void Main(string[] args)
    {
        const string appName = "RDPSecureApp";
        _mutex = new Mutex(true, appName, out bool createdNew);

        if (!createdNew)
        {
            // Application is already running
            if (Environment.UserInteractive)
            {
                MessageBox.Show(
                    "The application is already running.",
                    "RDPSecure",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            return;
        }

        try
        {
            // Determine if running as a service or application
            if (!Environment.UserInteractive)
            {
                // Running as a service
                ServiceBase[] ServicesToRun = new ServiceBase[]
                {
                    new RDPSecureService()
                };
                ServiceBase.Run(ServicesToRun);
            }
            else
            {
                // Running as a regular application
                ApplicationConfiguration.Initialize();
                Application.Run(new MainForm());
            }
        }
        catch (Exception ex)
        {
            // Log the error
            try
            {
                var logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "RDPSecure",
                    "error.log"
                );
                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
                File.AppendAllText(logPath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Error: {ex.Message}\n{ex.StackTrace}\n\n");
            }
            catch { }

            // Only show message box if running as application
            if (Environment.UserInteractive)
            {
                MessageBox.Show(
                    $"Application Error: {ex.Message}\n\nDetails: {ex.StackTrace}",
                    "RDPSecure Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }

    // Helper method to check if running as administrator
    public static bool IsAdministrator()
    {
        var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

    // Method to install the service
    public static void InstallService()
    {
        try
        {
            if (!IsAdministrator())
            {
                throw new Exception("Administrator privileges are required to install the service.");
            }

            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = $"create RDPSecure binPath= \"{Application.ExecutablePath}\" start= auto",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                // Set the description
                using var descProcess = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "sc",
                        Arguments = "description RDPSecure \"Monitors and protects RDP connections from brute force attacks\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                descProcess.Start();
                descProcess.WaitForExit();
            }
            else
            {
                throw new Exception($"Failed to install service: {result}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error installing service: {ex.Message}", ex);
        }
    }

    // Method to uninstall the service
    public static void UninstallService()
    {
        try
        {
            if (!IsAdministrator())
            {
                throw new Exception("Administrator privileges are required to uninstall the service.");
            }

            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = "delete RDPSecure",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Failed to uninstall service: {result}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error uninstalling service: {ex.Message}", ex);
        }
    }
}