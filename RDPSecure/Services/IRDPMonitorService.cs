using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDPSecure.Services
{
    public interface IRDPMonitorService
    {
        void StartMonitoring();
        void StopMonitoring();
        int GetRecentAttemptsCount();
        event EventHandler<LoginAttemptEventArgs> LoginAttemptDetected;
        event EventHandler<IPBanEventArgs> IPBanned;
    }
}