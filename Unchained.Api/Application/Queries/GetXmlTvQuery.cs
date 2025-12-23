using MediatR;
using Unchained.Application.Mapping;
using Unchained.Infrastructure.Epg;
using Unchained.Models;
using Unchained.Services.Channels;
using Unchained.Services.Epg;

namespace Unchained.Application.Queries;

public class GetXmlTvQuery : IRequest<GeneratedFileResult>
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public bool ForceRefresh { get; set; }
}

public class GetXmlTvQueryHandler : IRequestHandler<GetXmlTvQuery, GeneratedFileResult>
{
    private readonly IChannelService _channelService;
    private readonly IEpgService _epgService;
    private readonly XmlTvGenerator _generator;
    private readonly ILogger<GetXmlTvQueryHandler> _logger;

    public GetXmlTvQueryHandler(
        IChannelService channelService,
        IEpgService epgService,
        XmlTvGenerator generator,
        ILogger<GetXmlTvQueryHandler> logger)
    {
        _channelService = channelService;
        _epgService = epgService;
        _generator = generator;
        _logger = logger;
    }

    public async Task<GeneratedFileResult> Handle(GetXmlTvQuery request, CancellationToken cancellationToken)
    {
        var windowStart = request.From ?? DateTimeOffset.UtcNow.AddHours(-1);
        var windowEnd = request.To ?? DateTimeOffset.UtcNow.AddHours(48);

        var channels = await _channelService.GetChannelsAsync(request.ForceRefresh);
        var epgByChannel = await _epgService.GetEpgForChannelsAsync(channels.Select(c => c.Id.Value), windowStart, windowEnd, request.ForceRefresh);

        var epgEvents = epgByChannel.Values.SelectMany(list => list.Select(item => item.ToDomain())).ToList();
        var content = _generator.Generate(channels.ToList(), epgEvents);

        _logger.LogInformation("Generated XMLTV with {ChannelCount} channels and {EventCount} programmes", channels.Count, epgEvents.Count);

        return new GeneratedFileResult(
            content,
            "application/xml; charset=utf-8",
            "epg.xml");
    }
}
