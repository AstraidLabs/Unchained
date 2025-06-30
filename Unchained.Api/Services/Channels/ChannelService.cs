using Unchained.Models;

namespace Unchained.Services.Channels;

public class ChannelService : IChannelService
{
    private readonly IUnchained _magenta;
    private readonly ILogger<ChannelService> _logger;

    public ChannelService(IUnchained magenta, ILogger<ChannelService> logger)
    {
        _magenta = magenta;
        _logger = logger;
    }

    public async Task<List<ChannelDto>> GetChannelsAsync(bool forceRefresh = false)
    {
        await _magenta.InitializeAsync();
        return await _magenta.GetChannelsAsync(forceRefresh);
    }

    public async Task<string> GenerateM3UPlaylistAsync()
    {
        await _magenta.InitializeAsync();
        return await _magenta.GenerateM3UPlaylistAsync();
    }
}
