using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RDPSecure
{
    public static class IPValidator
    {
        // Regex for basic IPv4 format validation
        private static readonly Regex IPv4Regex = new Regex(
            @"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // IPv6 validation will be handled by IPAddress.TryParse as it's more complex

        public enum IPVersion
        {
            IPv4,
            IPv6,
            Unknown
        }

        public static (bool IsValid, string? ErrorMessage, IPVersion Version) ValidateIP(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return (false, "IP address cannot be empty", IPVersion.Unknown);
            }

            // Try parsing as IP address
            if (!IPAddress.TryParse(ipAddress, out IPAddress? parsedIP))
            {
                return (false, "Invalid IP address format", IPVersion.Unknown);
            }

            // Determine version and validate accordingly
            switch (parsedIP.AddressFamily)
            {
                case System.Net.Sockets.AddressFamily.InterNetwork:
                    if (!IPv4Regex.IsMatch(ipAddress))
                    {
                        return (false, "Invalid IPv4 address format", IPVersion.IPv4);
                    }
                    return (true, null, IPVersion.IPv4);

                case System.Net.Sockets.AddressFamily.InterNetworkV6:
                    // Validate specific IPv6 rules
                    if (IsInvalidIPv6(parsedIP))
                    {
                        return (false, "Invalid IPv6 address format", IPVersion.IPv6);
                    }
                    return (true, null, IPVersion.IPv6);

                default:
                    return (false, "Unsupported IP address type", IPVersion.Unknown);
            }
        }

        private static bool IsInvalidIPv6(IPAddress ip)
        {
            // Additional IPv6 validation rules
            string normalizedIP = ip.ToString();

            // Check for IPv4-mapped IPv6 addresses
            if (normalizedIP.Contains("::ffff:"))
            {
                return false; // These are valid
            }

            // Check for valid IPv6 scope ID
            if (normalizedIP.Contains('%'))
            {
                string[] parts = normalizedIP.Split('%');
                if (parts.Length != 2)
                {
                    return true; // Invalid scope format
                }
            }

            return false; // Passed all checks
        }

        public static bool IsReservedIP(IPAddress ip)
        {
            switch (ip.AddressFamily)
            {
                case System.Net.Sockets.AddressFamily.InterNetwork:
                    return IsReservedIPv4(ip);

                case System.Net.Sockets.AddressFamily.InterNetworkV6:
                    return IsReservedIPv6(ip);

                default:
                    return true; // Consider unknown types as reserved to be safe
            }
        }

        private static bool IsReservedIPv4(IPAddress ip)
        {
            byte[] bytes = ip.GetAddressBytes();

            // Check for loopback (127.0.0.0/8)
            if (bytes[0] == 127)
                return true;

            // Check for link-local (169.254.0.0/16)
            if (bytes[0] == 169 && bytes[1] == 254)
                return true;

            // Check for broadcast (255.255.255.255)
            if (bytes[0] == 255 && bytes[1] == 255 && bytes[2] == 255 && bytes[3] == 255)
                return true;

            // Check for "this" network (0.0.0.0/8)
            if (bytes[0] == 0)
                return true;

            return false;
        }

        private static bool IsReservedIPv6(IPAddress ip)
        {
            // Convert to string for easier comparison
            string normalizedIP = ip.ToString().ToLowerInvariant();

            // Check loopback (::1)
            if (normalizedIP == "::1")
                return true;

            // Check unspecified (::/128)
            if (normalizedIP == "::")
                return true;

            // Check link-local (fe80::/10)
            if (normalizedIP.StartsWith("fe8") ||
                normalizedIP.StartsWith("fe9") ||
                normalizedIP.StartsWith("fea") ||
                normalizedIP.StartsWith("feb"))
                return true;

            // Check unique local (fc00::/7)
            if (normalizedIP.StartsWith("fc") || normalizedIP.StartsWith("fd"))
                return true;

            return false;
        }

        public static string NormalizeIP(string ipAddress)
        {
            if (IPAddress.TryParse(ipAddress, out IPAddress? parsedIP))
            {
                return parsedIP.ToString();
            }
            return ipAddress;
        }
    }
}
