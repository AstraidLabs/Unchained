namespace Unchained.Services.Stream;

public class StreamService : IStreamService
{
    private readonly IUnchained _magenta;
    private readonly ILogger<StreamService> _logger;

    public StreamService(IUnchained magenta, ILogger<StreamService> logger)
    {
        _magenta = magenta;
        _logger = logger;
    }

    public async Task<string?> GetStreamUrlAsync(int channelId)
    {
        await _magenta.InitializeAsync();
        return await _magenta.GetStreamUrlAsync(channelId);
    }

    public async Task<string?> GetCatchupStreamUrlAsync(long scheduleId)
    {
        await _magenta.InitializeAsync();
        return await _magenta.GetCatchupStreamUrlAsync(scheduleId);
    }
}
