namespace Unchained.Tui.SignalR;

public record NotificationEvent(string Name, string Content, DateTimeOffset Timestamp);

public enum NotificationStatus
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting
}
