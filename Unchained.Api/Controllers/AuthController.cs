using Unchained.Application.Commands;
using Unchained.Models;
using Unchained.Models.Session;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Unchained.Extensions;
using Unchained.Services.Security;
using Microsoft.Extensions.Options;
using Unchained.Configuration;
using Unchained.Models.Auth;
using Unchained.Services.Session;

namespace Unchained.Controllers
{
    [ApiController]
    [Route("auth")]
    /// <summary>
    /// Handles authentication related endpoints such as logging in and out.
    /// All responses are returned using the <see cref="ApiResponse{T}"/> wrapper.
    /// </summary>
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AuthController> _logger;
        private readonly IInputSanitizer _sanitizer;
        private readonly IOptions<AuthOptions> _authOptions;
        private readonly ISessionManager _sessionManager;

        public AuthController(
            IMediator mediator,
            ILogger<AuthController> logger,
            IInputSanitizer sanitizer,
            IOptions<AuthOptions> authOptions,
            ISessionManager sessionManager)
        {
            _mediator = mediator;
            _logger = logger;
            _sanitizer = sanitizer;
            _authOptions = authOptions;
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// Central login endpoint that validates user credentials and creates
        /// a new application session when successful.
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        [ProducesResponseType(typeof(ApiResponse<string>), 401)]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<string>.ErrorResult("Validation failed", errors));
            }

            var sanitizedUser = _sanitizer.Sanitize(loginRequest.Username);

            using var securePassword = loginRequest.GetSecurePassword();

            var command = new LoginCommand
            {
                Username = sanitizedUser,
                Password = securePassword,
                RememberMe = loginRequest.RememberMe,
                SessionDurationHours = loginRequest.SessionDurationHours,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                UserAgent = HttpContext.Request.Headers.UserAgent.ToString()
            };

            var result = await _mediator.Send(command);

            if (result.Success && result.Data != null)
            {
                var cookieSettings = _authOptions.Value;
                SessionCookieHelper.SetSessionCookie(
                    Response,
                    result.Data.SessionId,
                    cookieSettings.CookieName,
                    cookieSettings.SecureCookies,
                    cookieSettings.SameSite,
                    cookieSettings.SessionTtlMinutes);

                var session = await _sessionManager.GetSessionAsync(result.Data.SessionId);
                var response = new LoginResponse
                {
                    Success = true,
                    DisplayName = sanitizedUser,
                    SessionExpiresAt = result.Data.ExpiresAt,
                    HasTokens = session?.HasValidTokens ?? false
                };

                return Ok(response);
            }

            return result.Message?.Contains("Invalid credentials") == true
                ? Unauthorized(result)
                : StatusCode(500, result);
        }

        /// <summary>
        /// Terminates the current session and removes the session cookie from
        /// the client.
        /// </summary>
        [HttpPost("logout")]
        [ProducesResponseType(typeof(LogoutResponse), 200)]
        public async Task<IActionResult> Logout()
        {
            var cookieSettings = _authOptions.Value;
            var sessionId = SessionCookieHelper.GetSessionId(Request, cookieSettings.CookieName);
            var command = new LogoutCommand { SessionId = sessionId };
            var result = await _mediator.Send(command);

            if (result.Success)
            {
                SessionCookieHelper.RemoveSessionCookie(Response, cookieSettings.CookieName);
            }

            return Ok(new LogoutResponse { Success = true });
        }


    }
}
