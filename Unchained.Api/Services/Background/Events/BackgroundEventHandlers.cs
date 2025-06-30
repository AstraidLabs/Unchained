using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Unchained.Hubs;

namespace Unchained.Services.Background.Events
{
    public class BackgroundEventHandlers :
        IEventHandler<WorkItemStartedEvent>,
        IEventHandler<WorkItemCompletedEvent>,
        IEventHandler<ServiceHealthChangedEvent>
    {
        private readonly ILogger<BackgroundEventHandlers> _logger;
        private readonly IHubContext<NotificationHub> _hub;

        public BackgroundEventHandlers(
            ILogger<BackgroundEventHandlers> logger,
            IHubContext<NotificationHub> hub)
        {
            _logger = logger;
            _hub = hub;
        }

        public async Task HandleAsync(WorkItemStartedEvent eventData, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Work item started: {WorkItemName} ({WorkItemId}) at {StartedAt}",
                eventData.WorkItemName, eventData.WorkItemId, eventData.StartedAt);

            await _hub.Clients.All.SendAsync("WorkItemStarted", eventData, cancellationToken);
        }

        public async Task HandleAsync(WorkItemCompletedEvent eventData, CancellationToken cancellationToken = default)
        {
            if (eventData.Success)
            {
                _logger.LogInformation("Work item completed successfully: {WorkItemName} in {Duration}",
                    eventData.WorkItemName, eventData.Duration);
            }
            else
            {
                _logger.LogWarning("Work item failed: {WorkItemName} - {ErrorMessage}",
                    eventData.WorkItemName, eventData.ErrorMessage);
            }

            await _hub.Clients.All.SendAsync("WorkItemCompleted", eventData, cancellationToken);
        }

        public async Task HandleAsync(ServiceHealthChangedEvent eventData, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Service health changed: {ServiceName} is now {Status} (Healthy: {IsHealthy})",
                eventData.ServiceName, eventData.Status, eventData.IsHealthy);

            await _hub.Clients.All.SendAsync("ServiceHealthChanged", eventData, cancellationToken);
        }
    }
}