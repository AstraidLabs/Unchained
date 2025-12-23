using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Unchained.Configuration;
using Unchained.Extensions;
using Unchained.Services.Session;

namespace Unchained.Middleware;

public class SessionValidationMiddleware
{
    private static readonly PathString[] PublicPaths =
    [
        "/auth/login",
        "/auth/logout",
        "/health/live"
    ];

    private readonly RequestDelegate _next;
    private readonly ISessionManager _sessionManager;
    private readonly AuthOptions _authOptions;
    private readonly ILogger<SessionValidationMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public SessionValidationMiddleware(
        RequestDelegate next,
        ISessionManager sessionManager,
        IOptions<AuthOptions> authOptions,
        IWebHostEnvironment environment,
        ILogger<SessionValidationMiddleware> logger)
    {
        _next = next;
        _sessionManager = sessionManager;
        _authOptions = authOptions.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsPublicPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var sessionId = SessionCookieHelper.GetSessionId(context.Request, _authOptions.CookieName);
        if (string.IsNullOrEmpty(sessionId))
        {
            await WriteUnauthorizedAsync(context, _authOptions.CookieName, "NoSession", "No session found");
            return;
        }

        var isValid = await _sessionManager.ValidateSessionAsync(sessionId);
        if (!isValid)
        {
            await WriteUnauthorizedAsync(context, _authOptions.CookieName, "SessionExpired", "Session is not valid or expired");
            return;
        }

        await _next(context);
    }

    private bool IsPublicPath(PathString path)
    {
        if (_environment.IsDevelopment() && path.HasValue && path.Value!.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return PublicPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task WriteUnauthorizedAsync(HttpContext context, string cookieName, string type, string detail)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.ContentType = "application/problem+json";
        context.Response.Cookies.Delete(cookieName);

        var payload = new
        {
            type,
            title = "Unauthorized",
            status = (int)HttpStatusCode.Unauthorized,
            detail
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
