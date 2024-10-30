using RDPSecure.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RDPSecure
{
    public class AuditPolicyManager
    {
        private readonly ISecurityLogger _logger;

        public AuditPolicyManager(ISecurityLogger logger)
        {
            _logger = logger;
        }

        public class AuditPolicyStatus
        {
            public bool LogonEnabled { get; set; }
            public bool OtherLogonEnabled { get; set; }
            public string LogonStatus { get; set; } = string.Empty;
            public string OtherLogonStatus { get; set; } = string.Empty;
        }

        public AuditPolicyStatus CheckAuditPolicies()
        {
            var status = new AuditPolicyStatus();

            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "auditpol.exe",
                        Arguments = "/get /subcategory:\"Logon\" /r",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Parse the output
                status.LogonEnabled = output.Contains("Success and Failure");
                status.LogonStatus = ParseAuditStatus(output);

                // Check Other Logon/Logoff Events
                process.StartInfo.Arguments = "/get /subcategory:\"Other Logon/Logoff Events\" /r";
                process.Start();
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                status.OtherLogonEnabled = output.Contains("Success and Failure");
                status.OtherLogonStatus = ParseAuditStatus(output);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error checking audit policies", ex);
                status.LogonStatus = "Error checking policy";
                status.OtherLogonStatus = "Error checking policy";
            }

            return status;
        }

        private string ParseAuditStatus(string output)
        {
            if (output.Contains("No auditing"))
                return "Disabled";
            if (output.Contains("Success and Failure"))
                return "Fully Enabled";
            if (output.Contains("Success"))
                return "Success Only";
            if (output.Contains("Failure"))
                return "Failure Only";
            return "Unknown";
        }

        public bool EnableAuditPolicies()
        {
            try
            {
                if (!Program.IsAdministrator())
                {
                    throw new UnauthorizedAccessException("Administrator privileges are required to modify audit policies.");
                }

                // Enable Logon auditing
                bool success = ExecuteAuditPolCommand("Logon");
                if (!success) return false;

                // Enable Other Logon/Logoff Events auditing
                success = ExecuteAuditPolCommand("Other Logon/Logoff Events");
                if (!success) return false;

                _logger.LogInformation("Audit policies successfully enabled");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error enabling audit policies", ex);
                return false;
            }
        }

        private bool ExecuteAuditPolCommand(string subcategory)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "auditpol.exe",
                        Arguments = $"/set /subcategory:\"{subcategory}\" /success:enable /failure:enable",
                        UseShellExecute = true,
                        Verb = "runas",
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing auditpol command for {subcategory}", ex);
                return false;
            }
        }
    }
}
