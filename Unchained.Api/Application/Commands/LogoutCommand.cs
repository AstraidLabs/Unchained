using Unchained.Models;
using MediatR;

namespace Unchained.Application.Commands
{
    public class LogoutCommand : IRequest<ApiResponse<string>>
    {
        public string? SessionId { get; set; }
    }
}