using Unchained.Models;
using Unchained.Services.Channels;
using MediatR;

namespace Unchained.Application.Queries
{
    public class GetChannelsQuery : IRequest<ApiResponse<List<ChannelDto>>>
    {
        public bool ForceRefresh { get; set; } = false;
    }

    public class GetChannelsQueryHandler : IRequestHandler<GetChannelsQuery, ApiResponse<List<ChannelDto>>>
    {
        private readonly IChannelService _channelService;
        private readonly ILogger<GetChannelsQueryHandler> _logger;

        public GetChannelsQueryHandler(IChannelService channelService, ILogger<GetChannelsQueryHandler> logger)
        {
            _channelService = channelService;
            _logger = logger;
        }

        public async Task<ApiResponse<List<ChannelDto>>> Handle(GetChannelsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var channels = await _channelService.GetChannelsAsync(request.ForceRefresh);
                return ApiResponse<List<ChannelDto>>.SuccessResult(channels, $"Found {channels.Count} channels");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get channels");
                return ApiResponse<List<ChannelDto>>.ErrorResult("Failed to get channels");
            }
        }
    }
}