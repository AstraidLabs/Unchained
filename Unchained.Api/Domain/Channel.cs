namespace Unchained.Domain;

public sealed class Channel
{
    public Channel(ChannelId id, string name, string? tvgId, string? logoUrl, bool hasArchive)
    {
        Id = id;
        Name = name;
        TvgId = string.IsNullOrWhiteSpace(tvgId) ? null : tvgId;
        LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl;
        HasArchive = hasArchive;
    }

    public ChannelId Id { get; }

    public string Name { get; }

    public string? TvgId { get; }

    public string? LogoUrl { get; }

    public bool HasArchive { get; }
}
