using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Unchained.Models;
using Unchained.Services.Channels;
using Unchained.Services.Epg;

namespace Unchained.Controllers;

[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    private readonly IChannelService _channelService;
    private readonly IEpgService _epgService;
    private readonly IMemoryCache _cache;
    private readonly GatewayRuntimeState _state;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IChannelService channelService,
        IEpgService epgService,
        IMemoryCache cache,
        GatewayRuntimeState state,
        ILogger<AdminController> logger)
    {
        _channelService = channelService;
        _epgService = epgService;
        _cache = cache;
        _state = state;
        _logger = logger;
    }

    [HttpPost("refresh/channels")]
    public async Task<IActionResult> RefreshChannels()
    {
        try
        {
            var channels = await _channelService.GetChannelsAsync(true);
            _state.MarkChannelsRefreshed();
            return Ok(new { message = $"Refreshed {channels.Count} channels" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Channel refresh failed");
            return Ok(new { message = "Channel refresh failed", error = ex.Message });
        }
    }

    [HttpPost("refresh/epg")]
    public async Task<IActionResult> RefreshEpg()
    {
        try
        {
            var channels = await _channelService.GetChannelsAsync();
            await _epgService.GetEpgForChannelsAsync(channels.Select(c => c.Id.Value), DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(48), true);
            _state.MarkEpgRefreshed();
            return Ok(new { message = "EPG refresh completed" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EPG refresh failed");
            return Ok(new { message = "EPG refresh failed", error = ex.Message });
        }
    }

    [HttpPost("cache/clear")]
    public IActionResult ClearCache()
    {
        try
        {
            if (_cache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0);
                _state.MarkCacheCleared();
            }
            return Ok(new { message = "Cache cleared" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache clear failed");
            return Ok(new { message = "Cache clear failed", error = ex.Message });
        }
    }
}
