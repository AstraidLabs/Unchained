using Unchained.Models;
using Unchained.Services.Session;
using MediatR;

namespace Unchained.Application.Queries
{
    public class GetSessionStatisticsQuery : IRequest<ApiResponse<SessionStatistics>>
    {
    }
}