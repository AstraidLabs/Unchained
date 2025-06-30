using MediatR;
using Unchained.Models;
using Unchained.Services.Stream;

namespace Unchained.Application.Commands;

public class StartRecordingCommandHandler : IRequestHandler<StartRecordingCommand, ApiResponse<string>>
{
    private readonly IDvrService _dvrService;
    private readonly ILogger<StartRecordingCommandHandler> _logger;

    public StartRecordingCommandHandler(IDvrService dvrService, ILogger<StartRecordingCommandHandler> logger)
    {
        _dvrService = dvrService;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(StartRecordingCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var fileName = $"recordings/channel_{request.ChannelId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.mp4";
            var jobId = await _dvrService.StartRecordingAsync(request.ChannelId, fileName, TimeSpan.FromMinutes(request.DurationMinutes), cancellationToken);
            if (string.IsNullOrEmpty(jobId))
            {
                return ApiResponse<string>.ErrorResult("Failed to start recording");
            }
            return ApiResponse<string>.SuccessResult(jobId, "Recording started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start recording for channel {ChannelId}", request.ChannelId);
            return ApiResponse<string>.ErrorResult("Failed to start recording");
        }
    }
}
