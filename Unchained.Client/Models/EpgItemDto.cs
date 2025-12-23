namespace Unchained.Client.Models;

public class EpgItemDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string Category { get; set; } = string.Empty;
    public long ScheduleId { get; set; }
    public int ChannelId { get; set; }
}
