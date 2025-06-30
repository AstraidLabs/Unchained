using MediatR;
using Unchained.Models;
using Unchained.Services.Ffmpeg;

namespace Unchained.Application.Queries;

public class GetRecordingStatusQuery : IRequest<ApiResponse<FfmpegJobStatus>>
{
    public string JobId { get; set; } = string.Empty;
}
