namespace RDPSecure.Models
{
    /// <summary>
    /// Represents a single login attempt record.
    /// </summary>
    public class LoginAttempt
    {
        public string IPAddress { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
