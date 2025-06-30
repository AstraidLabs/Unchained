namespace Unchained.Client.Models.Events;

public class ClientConnectedEvent
{
    public string ConnectionId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
