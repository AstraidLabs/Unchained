using Microsoft.AspNetCore.Http;

namespace Unchained.Extensions;

/// <summary>
/// Helper methods for working with session cookies
/// </summary>
public static class SessionCookieHelper
{
    /// <summary>
    /// Gets the session id from the request cookie or Authorization header
    /// </summary>
    public static string? GetSessionId(HttpRequest request, string cookieName)
    {
        // Try cookie first
        if (request.Cookies.TryGetValue(cookieName, out var cookieValue))
        {
            return cookieValue;
        }

        // Fallback to Authorization header
        var authHeader = request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Session "))
        {
            return authHeader.Substring("Session ".Length);
        }

        return null;
    }

    /// <summary>
    /// Sets the session cookie on the response
    /// </summary>
    public static void SetSessionCookie(HttpResponse response, string sessionId, string cookieName, bool secure, SameSiteMode sameSite, int ttlMinutes)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddMinutes(ttlMinutes)
        };

        response.Cookies.Append(cookieName, sessionId, cookieOptions);
    }

    /// <summary>
    /// Removes the session cookie from the response
    /// </summary>
    public static void RemoveSessionCookie(HttpResponse response, string cookieName)
    {
        response.Cookies.Delete(cookieName);
    }
}
