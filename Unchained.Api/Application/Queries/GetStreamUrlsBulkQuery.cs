using System.Collections.Generic;
using System.Linq;
using Unchained.Models;
using MediatR;

namespace Unchained.Application.Queries;

public class GetStreamUrlsBulkQuery : IRequest<ApiResponse<Dictionary<int, string?>>>
{
    public IEnumerable<int> ChannelIds { get; set; } = Enumerable.Empty<int>();
}
