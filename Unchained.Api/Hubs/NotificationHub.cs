using Microsoft.AspNetCore.SignalR;
using MediatR;
using Unchained.Application.Events;
using Unchained.Services.Connections;

namespace Unchained.Hubs
{
    /// <summary>
    /// SignalR hub used to broadcast simple notifications to all connected
    /// clients. At the moment it only exposes a single method that relays a
    /// message to every subscriber.
    /// </summary>
    public class NotificationHub : Hub
    {
        private readonly IMediator _mediator;
        private readonly IConnectionRegistry _registry;

        public NotificationHub(IMediator mediator, IConnectionRegistry registry)
        {
            _mediator = mediator;
            _registry = registry;
        }

        /// <summary>
        /// Sends a chat style message to all connected clients using MediatR.
        /// </summary>
        /// <param name="user">Name of the sender.</param>
        /// <param name="message">Message text.</param>
        public async Task SendMessage(string user, string message)
        {
            var evt = new ChatMessageReceivedEvent
            {
                User = user,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            await _mediator.Publish(evt);
        }

        public override async Task OnConnectedAsync()
        {
            var evt = new ConnectionOpenedEvent
            {
                ConnectionId = Context.ConnectionId,
                Timestamp = DateTime.UtcNow
            };

            _registry.Add(Context.ConnectionId);
            await _mediator.Publish(evt);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var evt = new ConnectionClosedEvent
            {
                ConnectionId = Context.ConnectionId,
                Timestamp = DateTime.UtcNow,
                Error = exception?.Message
            };

            _registry.Remove(Context.ConnectionId);
            await _mediator.Publish(evt);

            await base.OnDisconnectedAsync(exception);
        }

        public Task<IReadOnlyCollection<string>> GetConnections()
            => Task.FromResult(_registry.GetConnections());

        public Task<bool> IsConnected(string connectionId)
            => Task.FromResult(_registry.IsConnected(connectionId));
    }
}
