using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDPSecure.Data;

public class AppSettings
{
    public ProtectionSettings Protection { get; set; }
    public SystemSettings System { get; set; }
    public List<IPEntry> WhitelistedIPs { get; set; }

    public AppSettings()
    {
        Protection = new ProtectionSettings();
        System = new SystemSettings();
        WhitelistedIPs = new List<IPEntry>();
    }
}

public class ProtectionSettings
{
    public int MaxAttempts { get; set; }
    public int TimeWindow { get; set; }
    public int PrivateIPBanHours { get; set; }
    public int PublicIPBanDays { get; set; }
    public bool BurstProtectionEnabled { get; set; }

    public ProtectionSettings()
    {
        MaxAttempts = 3;
        TimeWindow = 5;
        PrivateIPBanHours = 1;
        PublicIPBanDays = 30;
        BurstProtectionEnabled = true;
    }
}

public class SystemSettings
{
    public int LogRetentionDays { get; set; }
    public bool StartWithWindows { get; set; }
    public bool MinimizeToTray { get; set; }

    public SystemSettings()
    {
        LogRetentionDays = 30;
        StartWithWindows = true;
        MinimizeToTray = true;
    }
}

public class IPEntry
{
    public string IPAddress { get; set; }
    public bool IsSubnet { get; set; }
    public string Type { get; set; }
    public DateTime AddedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsEnabled { get; set; }
    public string AddedBy { get; set; }
    public string Reason { get; set; }
    public int AttemptCount { get; set; }
    public SubnetUtils.SubnetInfo? SubnetInfo { get; set; }

    public IPEntry()
    {
        IPAddress = string.Empty;
        Type = "Whitelist";
        AddedDate = DateTime.Now;
        IsEnabled = true;
        AddedBy = "System";
        Reason = string.Empty;
        AttemptCount = 0;
        IsSubnet = false;
    }
}