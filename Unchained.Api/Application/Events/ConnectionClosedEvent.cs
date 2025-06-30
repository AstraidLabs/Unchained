using MediatR;

namespace Unchained.Application.Events;

public class ConnectionClosedEvent : INotification
{
    public string ConnectionId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Error { get; set; }
}
