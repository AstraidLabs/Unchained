using Unchained.Domain;

namespace Unchained.Infrastructure.Playlist;

public sealed class PlaylistChannel
{
    public PlaylistChannel(Channel channel, string streamUrl)
    {
        Channel = channel;
        StreamUrl = streamUrl;
    }

    public Channel Channel { get; }

    public string StreamUrl { get; }
}
