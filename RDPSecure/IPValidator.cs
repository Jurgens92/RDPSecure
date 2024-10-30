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
        // Regex for basic IPv4 format validation (xxx.xxx.xxx.xxx)
        private static readonly Regex IPv4Regex = new Regex(
            @"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static (bool IsValid, string? ErrorMessage) ValidateIPv4(string ipAddress)
        {
            // Check for null or empty
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return (false, "IP address cannot be empty");
            }

            // Check basic format using regex
            if (!IPv4Regex.IsMatch(ipAddress))
            {
                return (false, "Invalid IP address format. Must be in format: xxx.xxx.xxx.xxx where xxx is 0-255");
            }

            // Additional validation using IPAddress.TryParse
            if (!IPAddress.TryParse(ipAddress, out IPAddress? parsedIP))
            {
                return (false, "Invalid IP address");
            }

            // Ensure it's IPv4
            if (parsedIP.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return (false, "Only IPv4 addresses are supported");
            }

            return (true, null);
        }

        public static bool IsReservedIP(string ipAddress)
        {
            if (IPAddress.TryParse(ipAddress, out IPAddress? ip))
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
            return true; // If we can't parse it, consider it reserved to be safe
        }
    }
}
