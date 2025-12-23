using Unchained.Models;

namespace Unchained.Services.Epg;

public class EpgService : IEpgService
{
    private readonly IUnchained _magenta;
    private readonly ILogger<EpgService> _logger;

    public EpgService(IUnchained magenta, ILogger<EpgService> logger)
    {
        _magenta = magenta;
        _logger = logger;
    }

    public async Task<List<EpgItemDto>> GetEpgAsync(int channelId, DateTimeOffset? from = null, DateTimeOffset? to = null, bool forceRefresh = false)
    {
        await _magenta.InitializeAsync();
        return await _magenta.GetEpgAsync(channelId, from, to, forceRefresh);
    }

    public async Task<Dictionary<int, List<EpgItemDto>>> GetEpgForChannelsAsync(IEnumerable<int> channelIds, DateTimeOffset? from = null, DateTimeOffset? to = null, bool forceRefresh = false)
    {
        await _magenta.InitializeAsync();
        var tasks = channelIds.Select(async id =>
        {
            var epg = await _magenta.GetEpgAsync(id, from, to, forceRefresh);
            return (ChannelId: id, Epg: epg);
        });

        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(r => r.ChannelId, r => r.Epg);
    }
}
