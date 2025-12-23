using System;
using System.Collections.Generic;
using System.Linq;
using Unchained.Models;
using MediatR;

namespace Unchained.Application.Queries;

public class GetBulkEpgQuery : IRequest<ApiResponse<Dictionary<int, List<EpgItemDto>>>>
{
    public IEnumerable<int> ChannelIds { get; set; } = Enumerable.Empty<int>();
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public bool ForceRefresh { get; set; }
}
