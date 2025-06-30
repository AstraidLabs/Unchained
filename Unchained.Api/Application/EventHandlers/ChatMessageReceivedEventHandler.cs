using Unchained.Application.Events;
using Unchained.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Unchained.Application.EventHandlers;

/// <summary>
/// Handles <see cref="ChatMessageReceivedEvent"/> and broadcasts the message to all connected clients.
/// </summary>
public class ChatMessageReceivedEventHandler : INotificationHandler<ChatMessageReceivedEvent>
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<ChatMessageReceivedEventHandler> _logger;

    public ChatMessageReceivedEventHandler(
        IHubContext<NotificationHub> hubContext,
        ILogger<ChatMessageReceivedEventHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Broadcasting chat message from {User}", notification.User);
        await _hubContext.Clients.All.SendAsync("ReceiveMessage", notification.User, notification.Message, cancellationToken);
    }
}
