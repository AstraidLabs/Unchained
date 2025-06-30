using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Unchained.Application.Events;
using Unchained.Hubs;

namespace Unchained.Application.EventHandlers;

public class ConnectionOpenedEventHandler : INotificationHandler<ConnectionOpenedEvent>
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<ConnectionOpenedEventHandler> _logger;

    public ConnectionOpenedEventHandler(IHubContext<NotificationHub> hubContext, ILogger<ConnectionOpenedEventHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(ConnectionOpenedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Connection opened: {ConnectionId}", notification.ConnectionId);
        await _hubContext.Clients.All.SendAsync("ClientConnected", notification, cancellationToken);
    }
}
