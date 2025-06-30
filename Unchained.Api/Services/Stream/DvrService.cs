
using Unchained.Services.Ffmpeg;

namespace Unchained.Services.Stream;
public class DvrService : IDvrService
{
    private readonly IStreamService _streamService;
    private readonly IFFmpegService _ffmpegService;
    private readonly ILogger<DvrService> _logger;

    public DvrService(IStreamService streamService, IFFmpegService ffmpegService, ILogger<DvrService> logger)
    {
        _streamService = streamService;
        _ffmpegService = ffmpegService;
        _logger = logger;
    }

    public async Task<string?> StartRecordingAsync(int channelId, string outputFile, TimeSpan? duration = null, CancellationToken cancellationToken = default)
    {
        var url = await _streamService.GetStreamUrlAsync(channelId);
        if (string.IsNullOrEmpty(url))
        {
            _logger.LogWarning("Stream URL for channel {ChannelId} not found", channelId);
            return null;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (duration.HasValue)
            cts.CancelAfter(duration.Value);

        var jobId = await _ffmpegService.QueueConversionAsync(url, outputFile, cts.Token);
        _logger.LogInformation("Recording started for channel {ChannelId} -> {File}", channelId, outputFile);
        return jobId;
    }

    public FfmpegJobStatus? GetRecordingStatus(string jobId)
    {
        return _ffmpegService.GetJobStatus(jobId);
    }
}
