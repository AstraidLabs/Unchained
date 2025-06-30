using MediatR;

namespace Unchained.Application.Events;

public class ConnectionOpenedEvent : INotification
{
    public string ConnectionId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
