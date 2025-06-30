using Unchained.Models;
using Unchained.Models.Session;
using MediatR;

namespace Unchained.Application.Queries
{
    public class GetCurrentSessionQuery : IRequest<ApiResponse<SessionInfoDto>>
    {
        public string SessionId { get; set; } = string.Empty;
    }
}