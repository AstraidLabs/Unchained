using Unchained.Models;
using Unchained.Domain;

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

    public async Task<IReadOnlyCollection<Channel>> GetChannelsAsync(bool forceRefresh = false)
    {
        await _magenta.InitializeAsync();
        var channels = await _magenta.GetChannelsAsync(forceRefresh);
        return channels.Select(Map).ToList();
    }

    private static Channel Map(ChannelDto dto) =>
        new(new ChannelId(dto.ChannelId), dto.Name, dto.TvgId, dto.LogoUrl, dto.HasArchive);
}
