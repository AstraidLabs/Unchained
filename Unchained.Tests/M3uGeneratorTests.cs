using FluentAssertions;
using Unchained.Domain;
using Unchained.Infrastructure.Playlist;
using Xunit;

namespace Unchained.Tests;

public class M3uGeneratorTests
{
    [Fact]
    public void IncludesXmlTvHeaders()
    {
        var generator = new M3uGenerator();
        var channels = new[]
        {
            new PlaylistChannel(new Channel(new ChannelId(1), "Test Channel", "tvg-1", "http://logo.png", true), "http://localhost/stream/1")
        };

        var result = generator.Generate(channels, new Uri("http://localhost/xmltv"), PlaylistProfile.Generic);

        result.Should().Contain("#EXTM3U url-tvg=\"http://localhost/xmltv\" x-tvg-url=\"http://localhost/xmltv\"");
    }

    [Fact]
    public void EmitsCatchupOnlyForSupportedProfiles()
    {
        var generator = new M3uGenerator();
        var channels = new[]
        {
            new PlaylistChannel(new Channel(new ChannelId(7), "Archive Channel", null, null, true), "http://localhost/stream/7")
        };

        var kodi = generator.Generate(channels, new Uri("http://localhost/xmltv"), PlaylistProfile.Kodi);
        var tvheadend = generator.Generate(channels, new Uri("http://localhost/xmltv"), PlaylistProfile.Tvheadend);
        var jellyfin = generator.Generate(channels, new Uri("http://localhost/xmltv"), PlaylistProfile.Jellyfin);

        kodi.Should().Contain("catchup=\"default\"");
        tvheadend.Should().NotContain("catchup=\"default\"");
        jellyfin.Should().NotContain("catchup=\"default\"");
    }

    [Fact]
    public void SanitizesChannelNames()
    {
        var generator = new M3uGenerator();
        var channels = new[]
        {
            new PlaylistChannel(new Channel(new ChannelId(2), "Name\r\nWith Lines", null, null, false), "http://localhost/stream/2")
        };

        var result = generator.Generate(channels, new Uri("http://localhost/xmltv"), PlaylistProfile.Generic);

        result.Should().NotContain("\r").And.NotContain("\n");
        result.Should().Contain(",Name With Lines");
    }
}
