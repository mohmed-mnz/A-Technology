namespace Model;

public class BlockedAttemptLog
{
    public string IpAddress { get; set; } = string.Empty; 
    public string CountryCode { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } 
    public string UserAgent { get; set; } = string.Empty;
    public bool Blocked { get; set; }
}
