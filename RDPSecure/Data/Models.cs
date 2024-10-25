using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDPSecure.Data;

public class AppSettings
{
    public ProtectionSettings Protection { get; set; } = new();
    public SystemSettings System { get; set; } = new();
}

public class ProtectionSettings
{
    public int MaxAttempts { get; set; } = 3;
    public int TimeWindow { get; set; } = 5;
    public int PrivateIPBanHours { get; set; } = 1;
    public int PublicIPBanDays { get; set; } = 30;
    public bool BurstProtectionEnabled { get; set; } = true;
}

public class SystemSettings
{
    public int LogRetentionDays { get; set; } = 30;
    public bool StartWithWindows { get; set; } = true;
    public bool MinimizeToTray { get; set; } = true;
}

public class IPEntry
{
    public string IPAddress { get; set; } = string.Empty;
    public string Type { get; set; } = "Whitelist"; // Whitelist or Blacklist
    public DateTime AddedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string AddedBy { get; set; } = "System";
    public string Reason { get; set; } = string.Empty;
    public int AttemptCount { get; set; } = 0;
}