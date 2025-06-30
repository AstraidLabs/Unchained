using MediatR;

namespace Unchained.Application.Events;

/// <summary>
/// Notification raised when a chat message is sent via <see cref="NotificationHub"/>.
/// </summary>
public class ChatMessageReceivedEvent : INotification
{
    public string User { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
