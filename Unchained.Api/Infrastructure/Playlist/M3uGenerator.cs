using System.Globalization;
using System.Text;
using Unchained.Domain;

namespace Unchained.Infrastructure.Playlist;

public class M3uGenerator
{
    public string Generate(IEnumerable<PlaylistChannel> channels, Uri xmlTvUri, PlaylistProfile profile)
    {
        ArgumentNullException.ThrowIfNull(channels);
        ArgumentNullException.ThrowIfNull(xmlTvUri);

        var builder = new StringBuilder();
        builder.Append("#EXTM3U url-tvg=\"");
        builder.Append(xmlTvUri);
        builder.Append("\" x-tvg-url=\"");
        builder.Append(xmlTvUri);
        builder.AppendLine("\"");

        foreach (var channel in channels)
        {
            var sanitizedName = Sanitize(channel.Channel.Name);
            var resolvedId = ResolveChannelId(channel.Channel);
            builder.Append("#EXTINF:-1 ");
            builder.Append($"tvg-id=\"{resolvedId}\" ");
            builder.Append($"tvg-name=\"{sanitizedName}\"");

            if (ShouldEmitCatchup(profile) && channel.Channel.HasArchive)
            {
                builder.Append($" catchup=\"default\" catchup-source=\"/magenta/catchup/{channel.Channel.Id.Value}/" + "${start}-${end}\" catchup-days=\"7\"");
            }

            if (!string.IsNullOrWhiteSpace(channel.Channel.LogoUrl))
            {
                builder.Append($" tvg-logo=\"{channel.Channel.LogoUrl}\"");
            }

            builder.Append(',');
            builder.AppendLine(sanitizedName);
            builder.AppendLine(channel.StreamUrl);
        }

        return builder.ToString();
    }

    private static string ResolveChannelId(Channel channel) =>
        string.IsNullOrWhiteSpace(channel.TvgId)
            ? channel.Id.Value.ToString(CultureInfo.InvariantCulture)
            : channel.TvgId!;

    private static string Sanitize(string value) =>
        value.Replace("\r", " ").Replace("\n", " ").Trim();

    private static bool ShouldEmitCatchup(PlaylistProfile profile) =>
        profile is PlaylistProfile.Generic or PlaylistProfile.Kodi;
}
