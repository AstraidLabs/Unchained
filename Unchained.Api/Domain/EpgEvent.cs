namespace Unchained.Domain;

public sealed class EpgEvent
{
    public EpgEvent(
        ChannelId channelId,
        string title,
        string description,
        string category,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        long scheduleId)
    {
        ChannelId = channelId;
        Title = title;
        Description = description;
        Category = category;
        StartTime = startTime;
        EndTime = endTime;
        ScheduleId = scheduleId;
    }

    public ChannelId ChannelId { get; }

    public string Title { get; }

    public string Description { get; }

    public string Category { get; }

    public DateTimeOffset StartTime { get; }

    public DateTimeOffset EndTime { get; }

    public long ScheduleId { get; }
}
