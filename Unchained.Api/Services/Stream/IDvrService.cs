
using Unchained.Services.Ffmpeg;

namespace Unchained.Services.Stream;
public interface IDvrService
{
    Task<string?> StartRecordingAsync(int channelId, string outputFile, TimeSpan? duration = null, CancellationToken cancellationToken = default);
    FfmpegJobStatus? GetRecordingStatus(string jobId);
}
