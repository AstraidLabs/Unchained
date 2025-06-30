using Unchained.Models;
using Unchained.Models.Session;
using MediatR;

namespace Unchained.Application.Queries
{
    public class GetUserSessionsQuery : IRequest<ApiResponse<List<SessionDto>>>
    {
        public string CurrentSessionId { get; set; } = string.Empty;
    }
}