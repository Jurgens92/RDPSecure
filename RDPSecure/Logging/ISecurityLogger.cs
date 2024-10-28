using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDPSecure.Logging
{
    public interface ISecurityLogger
    {
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message, Exception? ex = null);
        void LogSecurityEvent(string ipAddress, string eventType, string details);
    }
}
