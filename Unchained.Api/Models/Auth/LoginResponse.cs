namespace Unchained.Models.Auth;

public class LoginResponse
{
    public bool Success { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTime SessionExpiresAt { get; set; }
    public bool HasTokens { get; set; }
}
