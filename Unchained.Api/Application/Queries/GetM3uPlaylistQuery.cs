using System.Text;
using MediatR;
using Unchained.Domain;
using Unchained.Infrastructure.Playlist;
using Unchained.Models;
using Unchained.Services.Channels;

namespace Unchained.Application.Queries;

public class GetM3uPlaylistQuery : IRequest<GeneratedFileResult>
{
    public required Uri XmlTvUri { get; init; }
    public required Uri StreamBaseUri { get; init; }
    public PlaylistProfile Profile { get; init; } = PlaylistProfile.Generic;
    public bool ForceRefresh { get; init; }
}

public class GetM3uPlaylistQueryHandler : IRequestHandler<GetM3uPlaylistQuery, GeneratedFileResult>
{
    private readonly IChannelService _channelService;
    private readonly M3uGenerator _generator;
    private readonly ILogger<GetM3uPlaylistQueryHandler> _logger;

    public GetM3uPlaylistQueryHandler(
        IChannelService channelService,
        M3uGenerator generator,
        ILogger<GetM3uPlaylistQueryHandler> logger)
    {
        _channelService = channelService;
        _generator = generator;
        _logger = logger;
    }

    public async Task<GeneratedFileResult> Handle(GetM3uPlaylistQuery request, CancellationToken cancellationToken)
    {
        var channels = await _channelService.GetChannelsAsync(request.ForceRefresh);
        var playlistChannels = channels
            .Select(c => new PlaylistChannel(c, new Uri(request.StreamBaseUri, c.Id.Value.ToString()).ToString()))
            .ToList();

        var content = _generator.Generate(playlistChannels, request.XmlTvUri, request.Profile);
        _logger.LogInformation("Generated M3U playlist with {ChannelCount} channels for profile {Profile}", playlistChannels.Count, request.Profile);

        return new GeneratedFileResult(
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content),
            "application/vnd.apple.mpegurl; charset=utf-8",
            "playlist.m3u");
    }
}
