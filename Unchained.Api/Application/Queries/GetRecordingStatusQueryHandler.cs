using MediatR;
using Unchained.Models;
using Unchained.Services.Stream;
using Unchained.Services.Ffmpeg;
using Microsoft.Extensions.Logging;

namespace Unchained.Application.Queries;

public class GetRecordingStatusQueryHandler : IRequestHandler<GetRecordingStatusQuery, ApiResponse<FfmpegJobStatus>>
{
    private readonly IDvrService _dvrService;
    private readonly ILogger<GetRecordingStatusQueryHandler> _logger;

    public GetRecordingStatusQueryHandler(IDvrService dvrService, ILogger<GetRecordingStatusQueryHandler> logger)
    {
        _dvrService = dvrService;
        _logger = logger;
    }

    public Task<ApiResponse<FfmpegJobStatus>> Handle(GetRecordingStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var status = _dvrService.GetRecordingStatus(request.JobId);
            if (status == null)
            {
                return Task.FromResult(ApiResponse<FfmpegJobStatus>.ErrorResult("Job not found"));
            }
            return Task.FromResult(ApiResponse<FfmpegJobStatus>.SuccessResult(status));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recording status for job {JobId}", request.JobId);
            return Task.FromResult(ApiResponse<FfmpegJobStatus>.ErrorResult("Failed to get status"));
        }
    }
}
