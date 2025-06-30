using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Unchained.Application.Events;
using Unchained.Hubs;

namespace Unchained.Application.EventHandlers;

public class ConnectionClosedEventHandler : INotificationHandler<ConnectionClosedEvent>
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<ConnectionClosedEventHandler> _logger;

    public ConnectionClosedEventHandler(IHubContext<NotificationHub> hubContext, ILogger<ConnectionClosedEventHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(ConnectionClosedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Connection closed: {ConnectionId}", notification.ConnectionId);
        await _hubContext.Clients.All.SendAsync("ClientDisconnected", notification, cancellationToken);
    }
}
