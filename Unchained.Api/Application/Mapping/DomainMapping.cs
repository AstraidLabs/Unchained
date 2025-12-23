using Unchained.Domain;
using Unchained.Models;

namespace Unchained.Application.Mapping;

public static class DomainMapping
{
    public static Channel ToDomain(this ChannelDto dto) =>
        new(new ChannelId(dto.ChannelId), dto.Name, dto.TvgId, dto.LogoUrl, dto.HasArchive);

    public static EpgEvent ToDomain(this EpgItemDto dto) =>
        new(
            new ChannelId(dto.ChannelId),
            dto.Title,
            dto.Description ?? string.Empty,
            dto.Category ?? string.Empty,
            dto.StartTime,
            dto.EndTime,
            dto.ScheduleId);
}
