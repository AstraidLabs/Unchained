using MediatR;

namespace Unchained.Application.Queries
{
    public class GenerateEpgXmlQuery : IRequest<string>
    {
        public int ChannelId { get; set; }
    }
}