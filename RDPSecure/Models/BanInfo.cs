namespace RDPSecure.Models
{
    /// <summary>
    /// Stores information about a banned IP address.
    /// </summary>
    public class BanInfo
    {
        public string IPAddress { get; set; } = string.Empty;
        public DateTime BanTime { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime ExpiryTime { get; set; }
        public int AttemptCount { get; set; }
        public string Location { get; set; } = "Detecting...";
        public IPValidator.IPVersion Version { get; set; }

        /// <summary>
        /// Helper property to determine if this is an IPv6 address.
        /// </summary>
        public bool IsIPv6 => Version == IPValidator.IPVersion.IPv6;

        /// <summary>
        /// Normalize the IP address when setting it.
        /// </summary>
        public void SetIPAddress(string ip)
        {
            IPAddress = IPValidator.NormalizeIP(ip);
            var (_, _, version) = IPValidator.ValidateIP(ip);
            Version = version;
        }
    }
}
