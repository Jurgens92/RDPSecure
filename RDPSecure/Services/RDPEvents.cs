using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDPSecure.Services
{
    public class LoginAttemptEventArgs : EventArgs
    {
        public string IPAddress { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    public class IPBanEventArgs : EventArgs
    {
        public string IPAddress { get; set; } = "";
        public DateTime BanTime { get; set; }
        public TimeSpan Duration { get; set; }
    }
}