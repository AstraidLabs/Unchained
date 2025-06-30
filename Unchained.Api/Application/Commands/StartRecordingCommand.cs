using MediatR;
using Unchained.Models;

namespace Unchained.Application.Commands;

public class StartRecordingCommand : IRequest<ApiResponse<string>>
{
    public int ChannelId { get; set; }
    public int DurationMinutes { get; set; } = 60;
}
