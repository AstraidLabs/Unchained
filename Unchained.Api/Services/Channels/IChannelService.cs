using Unchained.Models;

namespace Unchained.Services.Channels;

public interface IChannelService
{
    Task<List<ChannelDto>> GetChannelsAsync(bool forceRefresh = false);
    Task<string> GenerateM3UPlaylistAsync();
}
