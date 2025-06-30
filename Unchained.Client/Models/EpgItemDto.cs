namespace Unchained.Client.Models;

public class EpgItemDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Category { get; set; } = string.Empty;
    public long ScheduleId { get; set; }
}
