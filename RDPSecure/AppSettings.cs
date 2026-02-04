using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;

namespace RDPSecure
{
    public class AppSettings
    {
        // Protection Settings
        public int MaxAttempts { get; set; } = 3;
        public int TimeWindow { get; set; } = 5;
        public int PrivateIPBanHours { get; set; } = 1;
        public int PublicIPBanDays { get; set; } = 30;
        public bool GlobalBanlistEnabled { get; set; } = false;

        public GitHubSettings GitHub { get; set; }

        // IP Lists
        public List<IPEntry> WhitelistedIPs { get; set; }
        public List<IPEntry> BlacklistedIPs { get; set; }

        public AppSettings()
        {
            WhitelistedIPs = new List<IPEntry>();
            BlacklistedIPs = new List<IPEntry>();
            GitHub = new GitHubSettings();
        }
    }

    public class GitHubSettings
    {
        public string AccessToken { get; set; } = string.Empty;
        public int RefreshInterval { get; set; } = 30; // minutes
        public bool EnableRateLimitProtection { get; set; } = true;
    }


    public class IPEntry
    {
        public string IPAddress { get; set; } = string.Empty;
        public string Type { get; set; } = "Whitelist";
        public DateTime AddedDate { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string Notes { get; set; } = string.Empty;
        public bool IsSubnet { get; set; } = false;
        public int? PrefixLength { get; set; }
        public string? NetworkAddress { get; set; }
        public bool IsIPv6 { get; set; } = false;

        // Helper method to get subnet info if this is a subnet entry
        public SubnetUtils.SubnetInfo? GetSubnetInfo()
        {
            if (!IsSubnet || string.IsNullOrEmpty(NetworkAddress) || !PrefixLength.HasValue)
                return null;

            System.Net.IPAddress? network;
            if (!System.Net.IPAddress.TryParse(NetworkAddress, out network))
                return null;

            return new SubnetUtils.SubnetInfo(network, PrefixLength.Value);
        }

        // Helper method to check if an IP is within this entry's range
        public bool MatchesIP(string ipToCheck)
        {
            try
            {
                System.Net.IPAddress? checkIP;
                if (!System.Net.IPAddress.TryParse(ipToCheck, out checkIP))
                    return false;

                if (IsSubnet)
                {
                    var subnetInfo = GetSubnetInfo();
                    if (subnetInfo == null)
                        return false;

                    return SubnetUtils.IsIPInSubnet(checkIP, subnetInfo);
                }

                // For single IP comparison, normalize both addresses to handle IPv6 variations
                System.Net.IPAddress? entryIP;
                if (!System.Net.IPAddress.TryParse(IPAddress, out entryIP))
                    return false;

                // Compare normalized string representations (handles IPv6 case differences and compression)
                return entryIP.Equals(checkIP);
            }
            catch
            {
                return false;
            }
        }

        // Helper method to get the display address for UI purposes
        public string GetDisplayAddress()
        {
            if (IsSubnet && !string.IsNullOrEmpty(NetworkAddress) && PrefixLength.HasValue)
            {
                return $"{NetworkAddress}/{PrefixLength}";
            }
            return IPAddress;
        }

        // Helper method to check if this entry matches a display address (for removal)
        public bool MatchesDisplayAddress(string displayAddress)
        {
            return string.Equals(GetDisplayAddress(), displayAddress, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(IPAddress, displayAddress, StringComparison.OrdinalIgnoreCase);
        }

        // Helper method to set subnet information
        public void SetSubnetInfo(SubnetUtils.SubnetInfo subnetInfo)
        {
            IsSubnet = true;
            NetworkAddress = subnetInfo.NetworkAddress.ToString();
            PrefixLength = subnetInfo.PrefixLength;
            IsIPv6 = subnetInfo.IsIPv6;
            IPAddress = $"{NetworkAddress}/{PrefixLength}";
        }
    }
}