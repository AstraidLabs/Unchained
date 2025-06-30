using Unchained.Models;
using MediatR;

namespace Unchained.Application.Commands
{
    public class LogoutAllOtherSessionsCommand : IRequest<ApiResponse<string>>
    {
        public string CurrentSessionId { get; set; } = string.Empty;
    }
}