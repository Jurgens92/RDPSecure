using System.Diagnostics;
using RDPSecure.Logging;
using RDPSecure.Models;

namespace RDPSecure.Services
{
    /// <summary>
    /// Manages Windows Firewall rules for blocking malicious IPs.
    /// </summary>
    public class FirewallService
    {
        private const string FIREWALL_RULE_NAME = "RDPSecure-Blocked-IPs";
        private readonly ISecurityLogger _logger;

        public FirewallService(ISecurityLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Updates firewall rules to block the specified IPs.
        /// </summary>
        /// <param name="bannedIPs">Dictionary of banned IPs with their ban info</param>
        public void UpdateFirewallRules(IEnumerable<KeyValuePair<string, BanInfo>> bannedIPs)
        {
            try
            {
                // Remove existing rules
                RemoveFirewallRule("IPv4");
                RemoveFirewallRule("IPv6");

                var now = DateTime.UtcNow;
                var activeBans = bannedIPs
                    .Where(kvp => now < kvp.Value.ExpiryTime)
                    .ToList();

                // Separate IPv4 and IPv6 addresses
                var ipv4Bans = activeBans
                    .Where(b => !b.Value.IsIPv6)
                    .Select(b => b.Key)
                    .ToList();

                var ipv6Bans = activeBans
                    .Where(b => b.Value.IsIPv6)
                    .Select(b => b.Key)
                    .ToList();

                // Create IPv4 rule if needed
                if (ipv4Bans.Any())
                {
                    CreateFirewallRule(ipv4Bans, "IPv4");
                }

                // Create IPv6 rule if needed
                if (ipv6Bans.Any())
                {
                    CreateFirewallRule(ipv6Bans, "IPv6");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error managing firewall rules: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a firewall rule to block the specified IPs.
        /// </summary>
        private void CreateFirewallRule(List<string> ips, string version)
        {
            var ruleName = $"{FIREWALL_RULE_NAME}-{version}";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall add rule" +
                               $" name=\"{ruleName}\"" +
                               $" dir=in" +
                               $" interface=any" +
                               $" action=block" +
                               $" remoteip={string.Join(",", ips)}" +
                               (version == "IPv6" ? " protocol=IPv6" : ""),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                _logger.LogError($"Error creating {version} firewall rule. Output: {output} Error: {error}");
            }
            else
            {
                _logger.LogInformation($"{version} firewall rule created with {ips.Count} IPs");
            }
        }

        /// <summary>
        /// Removes a firewall rule by version.
        /// </summary>
        private void RemoveFirewallRule(string version)
        {
            try
            {
                var ruleName = $"{FIREWALL_RULE_NAME}-{version}";
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"advfirewall firewall delete rule name=\"{ruleName}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing {version} firewall rule: {ex.Message}");
            }
        }
    }
}
