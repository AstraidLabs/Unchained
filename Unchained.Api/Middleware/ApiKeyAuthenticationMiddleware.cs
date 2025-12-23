using Microsoft.Extensions.Options;
using Unchained.Configuration;

namespace Unchained.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private static readonly string[] ProtectedPrefixes = ["/channels", "/m3u", "/xmltv", "/health", "/status", "/admin"];
    private readonly RequestDelegate _next;
    private readonly GatewayAuthOptions _options;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        IOptions<GatewayOptions> options,
        ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _options = options.Value.Auth ?? new GatewayAuthOptions();
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_options.Mode == GatewayAuthMode.None || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            await _next(context);
            return;
        }

        if (!IsProtectedPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var headerName = string.IsNullOrWhiteSpace(_options.ApiKeyHeader) ? "X-Api-Key" : _options.ApiKeyHeader;
        if (!context.Request.Headers.TryGetValue(headerName, out var provided) ||
            !string.Equals(provided.ToString(), _options.ApiKey, StringComparison.Ordinal))
        {
            _logger.LogWarning("Rejected request to {Path} due to missing/invalid API key", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        await _next(context);
    }

    private static bool IsProtectedPath(PathString path) =>
        ProtectedPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
}
