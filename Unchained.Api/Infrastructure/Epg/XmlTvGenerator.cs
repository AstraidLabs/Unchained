using System.Text;
using System.Xml;
using Unchained.Domain;

namespace Unchained.Infrastructure.Epg;

public class XmlTvGenerator
{
    public byte[] Generate(IReadOnlyCollection<Channel> channels, IReadOnlyCollection<EpgEvent> events)
    {
        ArgumentNullException.ThrowIfNull(channels);
        ArgumentNullException.ThrowIfNull(events);

        var channelIds = channels.ToDictionary(c => c.Id, ResolveChannelId);
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            OmitXmlDeclaration = false,
            NewLineHandling = NewLineHandling.Replace
        };

        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("tv");

            foreach (var channel in channels)
            {
                var resolvedId = channelIds[channel.Id];
                writer.WriteStartElement("channel");
                writer.WriteAttributeString("id", resolvedId);
                writer.WriteElementString("display-name", channel.Name);
                if (!string.IsNullOrWhiteSpace(channel.LogoUrl))
                {
                    writer.WriteStartElement("icon");
                    writer.WriteAttributeString("src", channel.LogoUrl);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            foreach (var epgEvent in events.OrderBy(e => e.StartTime))
            {
                if (!channelIds.TryGetValue(epgEvent.ChannelId, out var channelId))
                {
                    continue;
                }

                writer.WriteStartElement("programme");
                writer.WriteAttributeString("start", FormatTimestamp(epgEvent.StartTime));
                writer.WriteAttributeString("stop", FormatTimestamp(epgEvent.EndTime));
                writer.WriteAttributeString("channel", channelId);
                writer.WriteElementString("title", epgEvent.Title);
                writer.WriteElementString("desc", epgEvent.Description ?? string.Empty);
                writer.WriteElementString("category", epgEvent.Category ?? string.Empty);
                writer.WriteElementString("scheduleId", epgEvent.ScheduleId.ToString());
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return stream.ToArray();
    }

    private static string ResolveChannelId(Channel channel) =>
        string.IsNullOrWhiteSpace(channel.TvgId) ? channel.Id.ToString() : channel.TvgId!;

    private static string FormatTimestamp(DateTimeOffset value)
    {
        var offset = value.Offset;
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var abs = offset.Duration();
        return $"{value:yyyyMMddHHmmss} {sign}{abs.Hours:00}{abs.Minutes:00}";
    }
}
