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

    public async Task<List<EpgItemDto>> GetEpgAsync(int channelId, DateTime? from = null, DateTime? to = null)
    {
        await _magenta.InitializeAsync();
        return await _magenta.GetEpgAsync(channelId, from, to);
    }

    public string GenerateXmlTv(List<EpgItemDto> epg, int channelId)
    {
        return _magenta.GenerateXmlTv(epg, channelId);
    }
}
