using Unchained.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace Unchained.Services.Background.Core
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue, IDisposable
    {
        private readonly PriorityQueue<BackgroundWorkItem, (int NegPriority, DateTime CreatedAt)> _workItems = new();
        private readonly object _lock = new();
        private readonly SemaphoreSlim _signal = new(0);
        private readonly ILogger<BackgroundTaskQueue> _logger;
        private readonly BackgroundServiceOptions _options;
        private volatile int _count = 0;

        public BackgroundTaskQueue(
            ILogger<BackgroundTaskQueue> logger,
            IOptions<BackgroundServiceOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        public int Count => _count;
        public int Capacity => _options.MaxQueueSize;

        public Task QueueBackgroundWorkItemAsync(BackgroundWorkItem workItem, CancellationToken cancellationToken = default)
        {
            if (workItem?.WorkItem == null)
                throw new ArgumentNullException(nameof(workItem));

            if (_count >= _options.MaxQueueSize)
            {
                _logger.LogWarning("Background task queue is full. Current count: {Count}, Max: {Max}",
                    _count, _options.MaxQueueSize);
                throw new InvalidOperationException("Background task queue is full");
            }

            lock (_lock)
            {
                _workItems.Enqueue(workItem, (-workItem.Priority, workItem.CreatedAt));
                Interlocked.Increment(ref _count);
            }

            _signal.Release();

            _logger.LogDebug("Queued background work item {Id} ({Name}) with priority {Priority}",
                workItem.Id, workItem.Name, workItem.Priority);

            return Task.CompletedTask;
        }

        public async Task<BackgroundWorkItem?> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);

            BackgroundWorkItem? item;
            lock (_lock)
            {
                if (!_workItems.TryDequeue(out item, out _))
                    return null;

                Interlocked.Decrement(ref _count);
            }

            if (item.ScheduledFor.HasValue &&
                item.ScheduledFor > DateTime.UtcNow)
            {
                lock (_lock)
                {
                    _workItems.Enqueue(item, (-item.Priority, item.CreatedAt));
                    Interlocked.Increment(ref _count);
                }
                _signal.Release();
                return null; // Will try again later
            }

            _logger.LogDebug("Dequeued background work item {Id} ({Name})",
                item.Id, item.Name);

            return item;
        }

        public IEnumerable<BackgroundWorkItem> GetQueuedItems()
        {
            lock (_lock)
            {
                return _workItems.UnorderedItems.Select(x => x.Element).ToList();
            }
        }

        public void Dispose()
        {
            _signal?.Dispose();
        }
    }
}
