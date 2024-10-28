using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDPSecure.Logging
{
    public class SecurityLogger : ISecurityLogger
    {
        public void LogInformation(string message)
        {
            Debug.WriteLine($"INFO: {message}");
        }

        public void LogWarning(string message)
        {
            Debug.WriteLine($"WARNING: {message}");
        }

        public void LogError(string message, Exception? ex = null)
        {
            Debug.WriteLine($"ERROR: {message}");
            if (ex != null)
                Debug.WriteLine($"Exception: {ex.Message}");
        }

        public void LogSecurityEvent(string ipAddress, string eventType, string details)
        {
            Debug.WriteLine($"SECURITY: IP={ipAddress}, Event={eventType}, Details={details}");
        }
    }
}