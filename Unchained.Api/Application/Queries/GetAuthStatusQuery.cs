using Unchained.Models;
using MediatR;

namespace Unchained.Application.Queries
{
    public class GetAuthStatusQuery : IRequest<ApiResponse<AuthStatusDto>>
    {
    }
}