using Unchained.Models;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Unchained.Application.Queries
{
    public class GetStreamUrlQuery : IRequest<ApiResponse<StreamUrlDto>>
    {
        public int ChannelId { get; set; }

        [RegularExpression("^p[1-5]$", ErrorMessage = "Quality must be between p1 and p5")]
        public string Quality { get; set; } = "p5";
    }
}
