using System.Collections.Generic;
using Unchained.Application.Commands;
using Unchained.Application.Queries;
using Unchained.Extensions;
using Unchained.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Net.Http.Headers;
using Unchained.Infrastructure.Playlist;

namespace Unchained.Controllers;

[ApiController]
[Route("magenta")]
/// <summary>
/// API endpoints that act as a thin wrapper over the underlying Unchained
/// service. Requires an active session for most operations.
/// </summary>
public class MagentaController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<MagentaController> _logger;

    public MagentaController(IMediator mediator, ILogger<MagentaController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Returns authentication status by combining current session details and
    /// stored token information.
    /// </summary>
    [HttpGet("auth/status")]
    [ProducesResponseType(typeof(ApiResponse<AuthStatusDto>), 200)]
    public async Task<IActionResult> GetAuthStatus()
    {
        var query = new GetAuthStatusQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves the list of available channels. Requires an active session.
    /// </summary>
    [HttpGet("channels")]
    [ProducesResponseType(typeof(ApiResponse<List<ChannelDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 401)]
    public async Task<IActionResult> GetChannels([FromQuery(Name = "refresh")] bool refresh = false)
    {
        try
        {
            // Session validation je řešena v SessionValidationBehavior
            var query = new GetChannelsQuery { ForceRefresh = refresh };
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ApiResponse<string>.ErrorResult("Authentication required",
                new List<string> { "Vyžaduje přihlášení" }));
        }
    }

    /// <summary>
    /// Retrieves specific channels by their IDs.
    /// </summary>
    [HttpGet("channels/bulk")]
    [ProducesResponseType(typeof(ApiResponse<List<ChannelDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 401)]
    public async Task<IActionResult> GetChannelsBulk([FromQuery(Name = "ids")] string ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
        {
            return BadRequest(ApiResponse<string>.ErrorResult("Invalid channel IDs"));
        }

        var parsed = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();

        if (parsed.Count == 0)
        {
            return BadRequest(ApiResponse<string>.ErrorResult("Invalid channel IDs"));
        }

        try
        {
            var query = new GetChannelsBulkQuery { ChannelIds = parsed };
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ApiResponse<string>.ErrorResult("Authentication required",
                new List<string> { "Vyžaduje přihlášení" }));
        }
    }

    /// <summary>
    /// Returns the Electronic Program Guide for the specified channel. Requires
    /// an active session.
    /// </summary>
    [HttpGet("epg/{channelId}")]
    [ProducesResponseType(typeof(ApiResponse<List<EpgItemDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 401)]
    [ProducesResponseType(typeof(ApiResponse<string>), 404)]
    public async Task<IActionResult> GetEpg(int channelId, [FromQuery] DateTimeOffset? from = null, [FromQuery] DateTimeOffset? to = null, [FromQuery(Name = "refresh")] bool refresh = false)
    {
        if (channelId <= 0)
        {
            return BadRequest(ApiResponse<string>.ErrorResult("Invalid channel ID",
                new List<string> { "ID kanálu musí být větší než 0" }));
        }

        try
        {
            var query = new GetEpgQuery
            {
                ChannelId = channelId,
                From = from,
                To = to,
                ForceRefresh = refresh
            };
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ApiResponse<string>.ErrorResult("Authentication required",
                new List<string> { "Vyžaduje přihlášení" }));
        }
    }

    /// <summary>
    /// Returns the Electronic Program Guide for multiple channels.
    /// </summary>
    [HttpGet("epg/bulk")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<int, List<EpgItemDto>>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 401)]
    public async Task<IActionResult> GetEpgBulk([FromQuery(Name = "ids")] string ids, [FromQuery] DateTimeOffset? from = null, [FromQuery] DateTimeOffset? to = null, [FromQuery(Name = "refresh")] bool refresh = false)
    {
        if (string.IsNullOrWhiteSpace(ids))
        {
            return BadRequest(ApiResponse<string>.ErrorResult("Invalid channel IDs"));
        }

        var parsed = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();

        if (parsed.Count == 0)
        {
            return BadRequest(ApiResponse<string>.ErrorResult("Invalid channel IDs"));
        }

        try
        {
            var query = new GetBulkEpgQuery
            {
                ChannelIds = parsed,
                From = from,
                To = to,
                ForceRefresh = refresh
            };
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ApiResponse<string>.ErrorResult("Authentication required",
                new List<string> { "Vyžaduje přihlášení" }));
        }
    }

    /// <summary>
    /// Retrieves the streaming URL for the given channel. An active session is
    /// required to access the stream.
    /// </summary>
    [HttpGet("stream/{channelId}")]
    [ProducesResponseType(typeof(ApiResponse<StreamUrlDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 401)]
    [ProducesResponseType(typeof(ApiResponse<string>), 404)]
    public async Task<IActionResult> GetStreamUrl(int channelId)
    {
        if (channelId <= 0)
        {
            return BadRequest(ApiResponse<string>.ErrorResult("Invalid channel ID",
                new List<string> { "ID kanálu musí být větší než 0" }));
        }

        try
        {
            var query = new GetStreamUrlQuery { ChannelId = channelId };
            var result = await _mediator.Send(query);

            if (!result.Success && result.Message?.Contains("not found") == true)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ApiResponse<string>.ErrorResult("Authentication required",
                new List<string> { "Vyžaduje přihlášení" }));
        }
    }

    /// <summary>
    /// Retrieves stream URLs for multiple channels.
    /// </summary>
    [HttpGet("stream/bulk")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<int, string?>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 401)]
    public async Task<IActionResult> GetStreamUrlsBulk([FromQuery(Name = "ids")] string ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
        {
            return BadRequest(ApiResponse<string>.ErrorResult("Invalid channel IDs"));
        }

        var parsed = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();

        if (parsed.Count == 0)
        {
            return BadRequest(ApiResponse<string>.ErrorResult("Invalid channel IDs"));
        }

        try
        {
            var query = new GetStreamUrlsBulkQuery { ChannelIds = parsed };
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ApiResponse<string>.ErrorResult("Authentication required",
                new List<string> { "Vyžaduje přihlášení" }));
        }
    }

    /// <summary>
    /// Retrieves the catch-up streaming URL for the specified schedule entry.
    /// Requires an active session.
    /// </summary>
    [HttpGet("catchup/{scheduleId}")]
    [ProducesResponseType(typeof(ApiResponse<StreamUrlDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 401)]
    [ProducesResponseType(typeof(ApiResponse<string>), 404)]
    public async Task<IActionResult> GetCatchupStream(long scheduleId)
    {
        if (scheduleId <= 0)
        {
            return BadRequest(ApiResponse<string>.ErrorResult("Invalid schedule ID",
                new List<string> { "ID pořadu musí být větší než 0" }));
        }

        try
        {
            var query = new GetCatchupStreamQuery { ScheduleId = scheduleId };
            var result = await _mediator.Send(query);

            if (!result.Success && result.Message?.Contains("not found") == true)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ApiResponse<string>.ErrorResult("Authentication required",
                new List<string> { "Vyžaduje přihlášení" }));
        }
    }

    /// <summary>
    /// Generates an M3U playlist for the authenticated user. Requires an active
    /// session to obtain channel data and streaming URLs.
    /// </summary>
    [HttpGet("m3u")]
    [OutputCache(Duration = 90, VaryByQueryKeys = new[] { "profile" })]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 401)]
    public async Task<IActionResult> GetPlaylist([FromQuery(Name = "profile")] string? profile = null, [FromQuery(Name = "refresh")] bool refresh = false)
    {
        try
        {
            var baseUri = new Uri($"{Request.Scheme}://{Request.Host}{Request.PathBase}");
            var xmlTvUri = new Uri(baseUri, "/magenta/xmltv");
            var streamBaseUri = new Uri(baseUri, "/magenta/stream/");
            var query = new GetM3uPlaylistQuery
            {
                Profile = ParseProfile(profile),
                XmlTvUri = xmlTvUri,
                StreamBaseUri = streamBaseUri,
                ForceRefresh = refresh
            };

            var result = await _mediator.Send(query);
            SetShortCacheHeaders();
            return File(result.Content, result.ContentType, result.FileName);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ApiResponse<string>.ErrorResult("Authentication required",
                new List<string> { "Vyžaduje přihlášení" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating playlist");
            return StatusCode(500, ApiResponse<string>.ErrorResult("Internal server error",
                new List<string> { "Došlo k chybě při generování playlistu" }));
        }
    }

    /// <summary>
    /// Exports the EPG as an XMLTV document for all channels.
    /// </summary>
    [HttpGet("xmltv")]
    [OutputCache(Duration = 90, VaryByQueryKeys = new[] { "from", "to" })]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 401)]
    public async Task<IActionResult> GetEpgXml([FromQuery] DateTimeOffset? from = null, [FromQuery] DateTimeOffset? to = null, [FromQuery(Name = "refresh")] bool refresh = false)
    {
        try
        {
            var query = new GetXmlTvQuery { From = from, To = to, ForceRefresh = refresh };
            var result = await _mediator.Send(query);
            SetShortCacheHeaders();

            return File(result.Content, result.ContentType, result.FileName);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ApiResponse<string>.ErrorResult("Authentication required",
                new List<string> { "Vyžaduje přihlášení" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating EPG XML");
            return StatusCode(500, ApiResponse<string>.ErrorResult("Internal server error",
                new List<string> { "Došlo k chybě při generování EPG XML" }));
        }
    }

    /// <summary>
    /// Returns devices associated with the current account.
    /// </summary>
    [HttpGet("devices")]
    [ProducesResponseType(typeof(ApiResponse<List<DeviceInfoDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 401)]
    public async Task<IActionResult> GetDevices()
    {
        try
        {
            var query = new GetDevicesQuery();
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ApiResponse<string>.ErrorResult("Authentication required",
                new List<string> { "Vyžaduje přihlášení" }));
        }
    }

    /// <summary>
    /// Deletes a registered device.
    /// </summary>
    [HttpDelete("devices/{id}")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 401)]
    public async Task<IActionResult> DeleteDevice(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(ApiResponse<string>.ErrorResult("Invalid device ID"));
        }

        try
        {
            var command = new DeleteDeviceCommand { DeviceId = id };
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ApiResponse<string>.ErrorResult("Authentication required",
                new List<string> { "Vyžaduje přihlášení" }));
        }
    }

    /// <summary>
    /// Performs a connectivity check against the Unchained API and returns
    /// information about the current session and token validity.
    /// </summary>
    [HttpGet("ping")]
    [ProducesResponseType(typeof(ApiResponse<PingResultDto>), 200)]
    public async Task<IActionResult> Ping()
    {
        var query = new PingQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    private PlaylistProfile ParseProfile(string? profile) =>
        Enum.TryParse<PlaylistProfile>(profile, true, out var parsed)
            ? parsed
            : PlaylistProfile.Generic;

    private void SetShortCacheHeaders()
    {
        Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromSeconds(90)
        };
        Response.Headers.Vary = "Accept-Encoding";
    }
}
