namespace Unchained.Models;

public class GatewayStatusDto
{
    public required string Service { get; init; }
    public required string Version { get; init; }
    public required string DataProvider { get; init; }
    public int ChannelCount { get; init; }
    public bool SignalRAvailable { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? LastChannelRefresh { get; init; }
    public DateTimeOffset? LastEpgRefresh { get; init; }
    public DateTimeOffset? LastCacheClear { get; init; }
}
