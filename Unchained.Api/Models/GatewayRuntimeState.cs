namespace Unchained.Models;

public class GatewayRuntimeState
{
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastChannelRefresh { get; private set; }
    public DateTimeOffset? LastEpgRefresh { get; private set; }
    public DateTimeOffset? LastCacheClear { get; private set; }

    public void MarkChannelsRefreshed() => LastChannelRefresh = DateTimeOffset.UtcNow;
    public void MarkEpgRefreshed() => LastEpgRefresh = DateTimeOffset.UtcNow;
    public void MarkCacheCleared() => LastCacheClear = DateTimeOffset.UtcNow;
}
