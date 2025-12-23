using System.ComponentModel.DataAnnotations;
using System.Security;
using Unchained.Extensions;

namespace Unchained.Models.Auth;

public class LoginRequest : IValidatableObject
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Range(1, 720)]
    public int? SessionDurationHours { get; set; }

    public bool RememberMe { get; set; }

    public SecureString GetSecurePassword() => Password.ToSecureString();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            yield return new ValidationResult("Username is required", new[] { nameof(Username) });
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            yield return new ValidationResult("Password is required", new[] { nameof(Password) });
        }
    }
}
