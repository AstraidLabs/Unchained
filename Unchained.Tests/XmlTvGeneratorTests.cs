using System.Text;
using System.Xml.Linq;
using FluentAssertions;
using Unchained.Domain;
using Unchained.Infrastructure.Epg;
using Xunit;

namespace Unchained.Tests;

public class XmlTvGeneratorTests
{
    [Fact]
    public void GeneratesUtf8WithoutBomAndCorrectProgrammeIds()
    {
        var channels = new List<Channel>
        {
            new(new ChannelId(1), "Channel One", "tvg-1", "http://logo.png", true),
            new(new ChannelId(2), "Channel Two", null, null, false)
        };

        var events = new List<EpgEvent>
        {
            new(new ChannelId(1), "Title", "Description", "Category",
                new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2024, 1, 1, 13, 0, 0, TimeSpan.FromHours(2)),
                123),
            new(new ChannelId(2), "Second", string.Empty, string.Empty,
                new DateTimeOffset(2024, 1, 1, 14, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2024, 1, 1, 15, 0, 0, TimeSpan.Zero),
                456)
        };

        var generator = new XmlTvGenerator();
        var bytes = generator.Generate(channels, events);
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };

        bytes.AsSpan(0, bom.Length).SequenceEqual(bom).Should().BeFalse();

        var xml = Encoding.UTF8.GetString(bytes);
        xml.Should().Contain("start=\"20240101120000 +0200\"");
        xml.Should().NotContain("+02:00");
        xml.Should().Contain("<icon src=\"http://logo.png\"");

        var doc = XDocument.Parse(xml);
        var channelIds = doc.Descendants("channel").Select(x => x.Attribute("id")?.Value).ToList();
        var programmeChannels = doc.Descendants("programme").Select(x => x.Attribute("channel")?.Value).ToList();

        programmeChannels.Should().OnlyContain(id => channelIds.Contains(id));
        channelIds.Should().Contain(new[] { "tvg-1", "2" });
    }
}
