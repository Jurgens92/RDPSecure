using System.Net;
using System.Net.Sockets;

namespace RDPSecure
{
    public class SubnetUtils
    {
        public class SubnetInfo
        {
            public IPAddress NetworkAddress { get; set; }
            public int PrefixLength { get; set; }
            public IPAddress SubnetMask { get; set; }
            public bool IsIPv6 { get; set; }

            public SubnetInfo(IPAddress networkAddress, int prefixLength)
            {
                NetworkAddress = networkAddress;
                PrefixLength = prefixLength;
                IsIPv6 = networkAddress.AddressFamily == AddressFamily.InterNetworkV6;
                SubnetMask = CreateSubnetMask(prefixLength, IsIPv6);
            }
        }

        public static (bool IsValid, string? ErrorMessage, SubnetInfo? SubnetInfo) ValidateSubnet(string input)
        {
            try
            {
                // Check if input is in CIDR notation
                string[] parts = input.Split('/');
                if (parts.Length != 2)
                {
                    return (false, "Invalid CIDR notation. Format should be 'IP/prefix' (e.g., 192.168.1.0/24)", null);
                }

                // Validate IP part
                if (!IPAddress.TryParse(parts[0], out IPAddress? networkAddress))
                {
                    return (false, "Invalid IP address format", null);
                }

                // Validate prefix length
                if (!int.TryParse(parts[1], out int prefixLength))
                {
                    return (false, "Invalid prefix length", null);
                }

                bool isIPv6 = networkAddress.AddressFamily == AddressFamily.InterNetworkV6;
                int maxPrefix = isIPv6 ? 128 : 32;

                if (prefixLength < 0 || prefixLength > maxPrefix)
                {
                    return (false, $"Prefix length must be between 0 and {maxPrefix}", null);
                }

                // Create and validate the subnet info
                var subnetInfo = new SubnetInfo(networkAddress, prefixLength);

                // Ensure the network address is valid for the subnet
                IPAddress normalizedNetwork = GetNetworkAddress(networkAddress, prefixLength);
                if (!networkAddress.Equals(normalizedNetwork))
                {
                    return (false, "Invalid network address for this subnet", null);
                }

                return (true, null, subnetInfo);
            }
            catch (Exception ex)
            {
                return (false, $"Error validating subnet: {ex.Message}", null);
            }
        }

        public static bool IsIPInSubnet(IPAddress ip, SubnetInfo subnet)
        {
            if (ip.AddressFamily != subnet.NetworkAddress.AddressFamily)
            {
                return false;
            }

            byte[] ipBytes = ip.GetAddressBytes();
            byte[] networkBytes = subnet.NetworkAddress.GetAddressBytes();
            byte[] maskBytes = subnet.SubnetMask.GetAddressBytes();

            for (int i = 0; i < ipBytes.Length; i++)
            {
                if ((ipBytes[i] & maskBytes[i]) != (networkBytes[i] & maskBytes[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static IPAddress CreateSubnetMask(int prefixLength, bool isIPv6)
        {
            int byteLength = isIPv6 ? 16 : 4;
            byte[] maskBytes = new byte[byteLength];

            for (int i = 0; i < byteLength; i++)
            {
                if (prefixLength >= 8)
                {
                    maskBytes[i] = 255;
                    prefixLength -= 8;
                }
                else if (prefixLength > 0)
                {
                    maskBytes[i] = (byte)(255 << (8 - prefixLength));
                    prefixLength = 0;
                }
                else
                {
                    maskBytes[i] = 0;
                }
            }

            return new IPAddress(maskBytes);
        }

        private static IPAddress GetNetworkAddress(IPAddress ip, int prefixLength)
        {
            byte[] ipBytes = ip.GetAddressBytes();
            byte[] maskBytes = CreateSubnetMask(prefixLength, ip.AddressFamily == AddressFamily.InterNetworkV6).GetAddressBytes();

            for (int i = 0; i < ipBytes.Length; i++)
            {
                ipBytes[i] &= maskBytes[i];
            }

            return new IPAddress(ipBytes);
        }

        public static string GetSubnetRange(SubnetInfo subnet)
        {
            if (subnet.IsIPv6)
            {
                return $"{subnet.NetworkAddress}/{subnet.PrefixLength}";
            }

            // For IPv4, calculate the number of addresses
            uint numAddresses = (uint)(Math.Pow(2, 32 - subnet.PrefixLength));
            byte[] networkBytes = subnet.NetworkAddress.GetAddressBytes();
            byte[] lastBytes = subnet.NetworkAddress.GetAddressBytes();

            // Calculate the last address in the range
            uint lastAddr = BitConverter.ToUInt32(networkBytes.Reverse().ToArray(), 0) + numAddresses - 1;
            byte[] lastAddrBytes = BitConverter.GetBytes(lastAddr).Reverse().ToArray();

            return $"{subnet.NetworkAddress} - {new IPAddress(lastAddrBytes)}";
        }

        public static int CalculateAddressesInSubnet(SubnetInfo subnet)
        {
            if (subnet.IsIPv6)
            {
                return subnet.PrefixLength == 128 ? 1 : -1; // -1 indicates "too large to count"
            }

            return (int)Math.Pow(2, 32 - subnet.PrefixLength);
        }
    }
}