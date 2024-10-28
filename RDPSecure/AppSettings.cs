namespace RDPSecure
{
    public class AppSettings
    {
        // Protection Settings
        public int MaxAttempts { get; set; } = 3;
        public int TimeWindow { get; set; } = 5;
        public int PrivateIPBanHours { get; set; } = 1;
        public int PublicIPBanDays { get; set; } = 30;
        public bool BurstProtectionEnabled { get; set; } = true;

        // IP Lists
        public List<IPEntry> WhitelistedIPs { get; set; }

        public AppSettings()
        {
            WhitelistedIPs = new List<IPEntry>();
        }
    }

    public class IPEntry
    {
        public string IPAddress { get; set; } = string.Empty;
        public string Type { get; set; } = "Whitelist";
        public DateTime AddedDate { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}