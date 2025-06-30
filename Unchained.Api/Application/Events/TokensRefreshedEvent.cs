using MediatR;

namespace Unchained.Application.Events
{
    public class TokensRefreshedEvent : INotification
    {
        public string Username { get; set; } = string.Empty;
        public DateTime NewExpiryTime { get; set; }
        public string SessionId { get; set; } = string.Empty;
    }
}
