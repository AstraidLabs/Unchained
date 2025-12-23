using Unchained.Models;

namespace Unchained.Services.Epg;

public interface IEpgService
{
    Task<List<EpgItemDto>> GetEpgAsync(int channelId, DateTimeOffset? from = null, DateTimeOffset? to = null, bool forceRefresh = false);
    Task<Dictionary<int, List<EpgItemDto>>> GetEpgForChannelsAsync(IEnumerable<int> channelIds, DateTimeOffset? from = null, DateTimeOffset? to = null, bool forceRefresh = false);
}
