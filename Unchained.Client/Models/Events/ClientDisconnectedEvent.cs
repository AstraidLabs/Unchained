namespace Unchained.Client.Models.Events;

public class ClientDisconnectedEvent
{
    public string ConnectionId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Error { get; set; }
}
