using Microsoft.AspNetCore.Http;

namespace Unchained.Configuration;

public class AuthOptions
{
    public const string SectionName = "Auth";

    public string CookieName { get; set; } = "Unchained.Session";

    /// <summary>
    /// Session lifetime in minutes for issued cookies.
    /// </summary>
    public int SessionTtlMinutes { get; set; } = 480;

    /// <summary>
    /// Use secure cookies (HTTPS only).
    /// </summary>
    public bool SecureCookies { get; set; } = true;

    /// <summary>
    /// SameSite mode for session cookies.
    /// </summary>
    public SameSiteMode SameSite { get; set; } = SameSiteMode.Lax;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(CookieName))
        {
            throw new ArgumentException("Auth:CookieName must be provided");
        }

        if (SessionTtlMinutes < 1)
        {
            throw new ArgumentException("Auth:SessionTtlMinutes must be at least 1 minute");
        }
    }
}
