using System.Collections.Generic;
using System.Linq;
using Unchained.Models;
using Unchained.Services.Channels;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Unchained.Application.Queries;

public class GetChannelsBulkQuery : IRequest<ApiResponse<List<ChannelDto>>>
{
    public IEnumerable<int> ChannelIds { get; set; } = Enumerable.Empty<int>();
}

public class GetChannelsBulkQueryHandler : IRequestHandler<GetChannelsBulkQuery, ApiResponse<List<ChannelDto>>>
{
    private readonly IChannelService _channelService;
    private readonly ILogger<GetChannelsBulkQueryHandler> _logger;

    public GetChannelsBulkQueryHandler(IChannelService channelService, ILogger<GetChannelsBulkQueryHandler> logger)
    {
        _channelService = channelService;
        _logger = logger;
    }

    public async Task<ApiResponse<List<ChannelDto>>> Handle(GetChannelsBulkQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var channels = await _channelService.GetChannelsAsync();
            var filtered = channels
                .Where(c => request.ChannelIds.Contains(c.Id.Value))
                .Select(c => new ChannelDto
                {
                    ChannelId = c.Id.Value,
                    TvgId = c.TvgId ?? c.Id.ToString(),
                    Name = c.Name,
                    LogoUrl = c.LogoUrl ?? string.Empty,
                    HasArchive = c.HasArchive
                })
                .ToList();
            return ApiResponse<List<ChannelDto>>.SuccessResult(filtered, $"Found {filtered.Count} channels");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get channels");
            return ApiResponse<List<ChannelDto>>.ErrorResult("Failed to get channels");
        }
    }
}
