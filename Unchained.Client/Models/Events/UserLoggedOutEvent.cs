namespace Unchained.Client.Models.Events;

public class UserLoggedOutEvent
{
    public string Username { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Reason { get; set; } = string.Empty;
}
