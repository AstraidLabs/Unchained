using Unchained.Models;
using MediatR;


namespace Unchained.Application.Queries
{
    public class PingQuery : IRequest<ApiResponse<PingResultDto>>
    {
    }
}