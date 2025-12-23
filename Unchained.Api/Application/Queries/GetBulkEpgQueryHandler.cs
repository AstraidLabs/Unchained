using System;
using System.Collections.Generic;
using System.Linq;
using Unchained.Models;
using Unchained.Services.Epg;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Unchained.Application.Queries;

public class GetBulkEpgQueryHandler : IRequestHandler<GetBulkEpgQuery, ApiResponse<Dictionary<int, List<EpgItemDto>>>>
{
    private readonly IEpgService _epgService;
    private readonly ILogger<GetBulkEpgQueryHandler> _logger;

    public GetBulkEpgQueryHandler(IEpgService epgService, ILogger<GetBulkEpgQueryHandler> logger)
    {
        _epgService = epgService;
        _logger = logger;
    }

    public async Task<ApiResponse<Dictionary<int, List<EpgItemDto>>>> Handle(GetBulkEpgQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var dict = await _epgService.GetEpgForChannelsAsync(request.ChannelIds, request.From, request.To, request.ForceRefresh);

            return ApiResponse<Dictionary<int, List<EpgItemDto>>>.SuccessResult(dict,
                $"Retrieved EPG for {dict.Count} channels");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get bulk EPG");
            return ApiResponse<Dictionary<int, List<EpgItemDto>>>.ErrorResult("Failed to get EPG");
        }
    }
}
