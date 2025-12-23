using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;
using Unchained.Application.Queries;
using Unchained.Configuration;
using Unchained.Infrastructure.Epg;
using Unchained.Infrastructure.Playlist;
using Unchained.Domain;
using Unchained.Models;
using Unchained.Services.Channels;
using Unchained.Services.Epg;

namespace Unchained.Controllers;

[ApiController]
[Route("")]
public class GatewayController : ControllerBase
{
    private readonly IChannelService _channelService;
    private readonly IEpgService _epgService;
    private readonly IMediator _mediator;
    private readonly M3uGenerator _m3uGenerator;
    private readonly XmlTvGenerator _xmlTvGenerator;
    private readonly GatewayRuntimeState _state;
    private readonly GatewayOptions _options;
    private readonly ILogger<GatewayController> _logger;

    public GatewayController(
        IChannelService channelService,
        IEpgService epgService,
        IMediator mediator,
        M3uGenerator m3uGenerator,
        XmlTvGenerator xmlTvGenerator,
        GatewayRuntimeState state,
        IOptions<GatewayOptions> options,
        ILogger<GatewayController> logger)
    {
        _channelService = channelService;
        _epgService = epgService;
        _mediator = mediator;
        _m3uGenerator = m3uGenerator;
        _xmlTvGenerator = xmlTvGenerator;
        _state = state;
        _options = options.Value;
        _logger = logger;
    }

    [HttpGet("channels")]
    [ProducesResponseType(typeof(IEnumerable<GatewayChannelDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChannels()
    {
        try
        {
            var channels = await _channelService.GetChannelsAsync();
            _state.MarkChannelsRefreshed();

            var dto = channels.Select(c => new GatewayChannelDto
            {
                Id = c.Id.Value,
                Name = c.Name,
                GroupTitle = "Unchained",
                TvgId = string.IsNullOrWhiteSpace(c.TvgId) ? c.Id.Value.ToString() : c.TvgId,
                TvgName = string.IsNullOrWhiteSpace(c.TvgId) ? c.Name : c.TvgId,
                TvgLogo = c.LogoUrl ?? string.Empty,
                HasArchive = c.HasArchive
            });

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load channels, returning empty list");
            return Ok(Array.Empty<GatewayChannelDto>());
        }
    }

    [HttpGet("m3u")]
    [OutputCache(PolicyName = "GatewayPlaylist")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlaylist([FromQuery] string? profile = null)
    {
        var resolvedProfile = ParseProfile(profile);
        var baseUri = ResolveBaseUri();
        var xmlTvUri = new Uri(baseUri, "/xmltv");
        var streamBaseUri = new Uri(baseUri, "/magenta/stream/");

        try
        {
            var result = await _mediator.Send(new GetM3uPlaylistQuery
            {
                Profile = resolvedProfile,
                XmlTvUri = xmlTvUri,
                StreamBaseUri = streamBaseUri
            });

            Response.ContentType = "application/vnd.apple.mpegurl";
            return File(result.Content, "application/vnd.apple.mpegurl", result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falling back to empty playlist");
            var empty = _m3uGenerator.Generate(Array.Empty<PlaylistChannel>(), xmlTvUri, resolvedProfile);
            return File(System.Text.Encoding.UTF8.GetBytes(empty), "application/vnd.apple.mpegurl", "playlist.m3u");
        }
    }

    [HttpGet("xmltv")]
    [OutputCache(PolicyName = "GatewayXmlTv")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetXmlTv([FromQuery] DateTimeOffset? from = null, [FromQuery] DateTimeOffset? to = null)
    {
        var windowStart = from ?? DateTimeOffset.UtcNow.AddHours(-1);
        var windowEnd = to ?? DateTimeOffset.UtcNow.AddHours(48);

        try
        {
            var result = await _mediator.Send(new GetXmlTvQuery
            {
                From = windowStart,
                To = windowEnd
            });
            _state.MarkEpgRefreshed();

            Response.ContentType = "application/xml";
            return File(result.Content, "application/xml", result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falling back to empty XMLTV");
            var empty = _xmlTvGenerator.Generate(Array.Empty<Domain.Channel>(), Array.Empty<Domain.EpgEvent>());
            return File(empty, "application/xml", "epg.xml");
        }
    }

    [HttpGet("health/live")]
    public IActionResult LiveHealth() => Ok(new { status = "ok", time = DateTimeOffset.UtcNow });

    [HttpGet("health/ready")]
    public async Task<IActionResult> ReadyHealth()
    {
        try
        {
            await _channelService.GetChannelsAsync();
            return Ok(new { status = "ready", time = DateTimeOffset.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Readiness degraded");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "degraded", message = ex.Message });
        }
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(GatewayStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus()
    {
        int channelCount = 0;
        string provider = "empty/mock";

        try
        {
            var channels = await _channelService.GetChannelsAsync();
            channelCount = channels.Count;
            provider = channelCount > 0 ? "Unchained" : "empty/mock";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Status channel lookup failed");
        }

        var status = new GatewayStatusDto
        {
            Service = "Unchained Gateway",
            Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
            DataProvider = provider,
            ChannelCount = channelCount,
            SignalRAvailable = _options.SignalREnabled,
            StartedAt = _state.StartedAt,
            LastChannelRefresh = _state.LastChannelRefresh,
            LastEpgRefresh = _state.LastEpgRefresh,
            LastCacheClear = _state.LastCacheClear
        };

        return Ok(status);
    }

    private PlaylistProfile ParseProfile(string? profile) =>
        Enum.TryParse<PlaylistProfile>(profile, true, out var parsed)
            ? parsed
            : PlaylistProfile.Generic;

    private Uri ResolveBaseUri()
    {
        if (!string.IsNullOrWhiteSpace(_options.BaseUrl) && Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var configured))
        {
            return configured;
        }

        return new Uri($"{Request.Scheme}://{Request.Host}{Request.PathBase}");
    }
}
