namespace MercatoApp.Services;

/// <summary>
/// Result of password validation.
/// </summary>
public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Interface for password validation service.
/// </summary>
public interface IPasswordValidationService
{
    /// <summary>
    /// Validates a password against the security policy.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <returns>Validation result with any errors.</returns>
    PasswordValidationResult Validate(string password);
}

/// <summary>
/// Service for validating passwords against security policy.
/// </summary>
public class PasswordValidationService : IPasswordValidationService
{
    private const int MinLength = 8;
    private const int MaxLength = 128;

    // Special characters that are allowed in passwords
    private const string SpecialCharacters = "!@#$%^&*(),.?\"':{}|<>_-+=[]\\;`~";

    // Common passwords list (subset of most common passwords)
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "12345678", "123456789", "1234567890", "qwerty123",
        "password1", "password123", "abc123456", "letmein123", "welcome1",
        "admin123", "iloveyou1", "monkey123", "dragon123", "master123",
        "sunshine1", "princess1", "qwertyuiop", "football1", "baseball1"
    };

    /// <inheritdoc />
    public PasswordValidationResult Validate(string password)
    {
        var result = new PasswordValidationResult { IsValid = true };

        if (string.IsNullOrEmpty(password))
        {
            result.IsValid = false;
            result.Errors.Add("Password is required.");
            return result;
        }

        if (password.Length < MinLength)
        {
            result.IsValid = false;
            result.Errors.Add($"Password must be at least {MinLength} characters long.");
        }

        if (password.Length > MaxLength)
        {
            result.IsValid = false;
            result.Errors.Add($"Password must not exceed {MaxLength} characters.");
        }

        if (!password.Any(char.IsUpper))
        {
            result.IsValid = false;
            result.Errors.Add("Password must contain at least one uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            result.IsValid = false;
            result.Errors.Add("Password must contain at least one lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            result.IsValid = false;
            result.Errors.Add("Password must contain at least one digit.");
        }

        if (!password.Any(c => SpecialCharacters.Contains(c)))
        {
            result.IsValid = false;
            result.Errors.Add("Password must contain at least one special character.");
        }

        if (CommonPasswords.Contains(password))
        {
            result.IsValid = false;
            result.Errors.Add("This password is too common. Please choose a more unique password.");
        }

        return result;
    }
}
