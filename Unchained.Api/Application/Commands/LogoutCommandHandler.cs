using Unchained.Services.Session;
using Unchained.Services.TokenStorage;
using Unchained.Services;
using MediatR;
using Unchained.Application.Events;
using Unchained.Models;
using Microsoft.Extensions.Logging;

namespace Unchained.Application.Commands
{
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, ApiResponse<string>>
    {
        private readonly ISessionManager _sessionManager;
        private readonly ITokenStorage _tokenStorage;
        private readonly IUnchained _magentaService;
        private readonly IMediator _mediator;
        private readonly ILogger<LogoutCommandHandler> _logger;

        public LogoutCommandHandler(
            ISessionManager sessionManager,
            ITokenStorage tokenStorage,
            IUnchained magentaService,
            IMediator mediator,
            ILogger<LogoutCommandHandler> logger)
        {
            _sessionManager = sessionManager;
            _tokenStorage = tokenStorage;
            _magentaService = magentaService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<ApiResponse<string>> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _magentaService.InitializeAsync();
                string? username = null;

                // 1. Ukončíme session pokud existuje
                if (!string.IsNullOrEmpty(request.SessionId))
                {
                    var session = await _sessionManager.GetSessionAsync(request.SessionId);
                    username = session?.Username;

                    await _sessionManager.RemoveSessionAsync(request.SessionId);
                }

                // 2. Vymažeme Unchained tokeny pro tuto session
                if (!string.IsNullOrEmpty(request.SessionId))
                {
                    await _tokenStorage.ClearTokensAsync(request.SessionId);
                }

                // 3. Zavoláme logout na Magenta service
                await _magentaService.LogoutAsync(request.SessionId ?? string.Empty);

                // 4. Publikujeme event
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(request.SessionId))
                {
                    await _mediator.Publish(new UserLoggedOutEvent
                    {
                        Username = username,
                        SessionId = request.SessionId,
                        Timestamp = DateTime.UtcNow,
                        Reason = "Voluntary"
                    }, cancellationToken);
                }

                _logger.LogInformation("User logged out successfully");
                return ApiResponse<string>.SuccessResult("Logout successful", "Odhlášení proběhlo úspěšně");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout error");
                return ApiResponse<string>.ErrorResult("Internal server error",
                    new List<string> { "Došlo k chybě při odhlašování" });
            }
        }
    }
}
