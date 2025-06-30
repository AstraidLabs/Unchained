namespace Unchained.Client.Models.Events;

public class UserLoggedInEvent
{
    public string Username { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; } = string.Empty;
}
