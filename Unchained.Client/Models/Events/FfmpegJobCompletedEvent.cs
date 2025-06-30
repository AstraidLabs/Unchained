namespace Unchained.Client.Models.Events;

public class FfmpegJobCompletedEvent
{
    public string JobId { get; set; } = string.Empty;
    public string OutputFile { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
