using MediatR;
using Microsoft.AspNetCore.Mvc;
using Unchained.Application.Commands;
using Unchained.Application.Queries;
using Unchained.Models;
using Unchained.Services.Ffmpeg;

namespace Unchained.Controllers;

[ApiController]
[Route("dvr")]
public class DvrController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DvrController> _logger;

    public DvrController(IMediator mediator, ILogger<DvrController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("start/{channelId}")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public async Task<IActionResult> StartRecording(int channelId, [FromQuery] int minutes = 60)
    {
        var command = new StartRecordingCommand { ChannelId = channelId, DurationMinutes = minutes };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("status/{jobId}")]
    [ProducesResponseType(typeof(ApiResponse<FfmpegJobStatus>), 200)]
    public async Task<IActionResult> GetStatus(string jobId)
    {
        var query = new GetRecordingStatusQuery { JobId = jobId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
