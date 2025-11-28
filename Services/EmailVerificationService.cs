using System.Security.Cryptography;
using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Result of an email verification attempt.
/// </summary>
public class EmailVerificationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool TokenExpired { get; set; }
    public bool TokenAlreadyUsed { get; set; }
    public User? User { get; set; }
}

/// <summary>
/// Interface for email verification service.
/// </summary>
public interface IEmailVerificationService
{
    /// <summary>
    /// Verifies the user's email using the provided token.
    /// </summary>
    /// <param name="token">The verification token.</param>
    /// <returns>The verification result.</returns>
    Task<EmailVerificationResult> VerifyEmailAsync(string token);

    /// <summary>
    /// Generates a new verification token for the user.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <returns>The new verification token, or null if user not found.</returns>
    Task<string?> GenerateNewVerificationTokenAsync(string email);

    /// <summary>
    /// Gets the verification token expiry duration.
    /// </summary>
    TimeSpan TokenExpiryDuration { get; }
}

/// <summary>
/// Service for handling email verification with time-limited tokens.
/// </summary>
public class EmailVerificationService : IEmailVerificationService
{
    private const int VerificationTokenSizeBytes = 32;
    private static readonly TimeSpan DefaultTokenExpiryDuration = TimeSpan.FromHours(24);

    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailVerificationService> _logger;

    public EmailVerificationService(
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<EmailVerificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    /// <inheritdoc />
    public TimeSpan TokenExpiryDuration => DefaultTokenExpiryDuration;

    /// <inheritdoc />
    public async Task<EmailVerificationResult> VerifyEmailAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new EmailVerificationResult
            {
                Success = false,
                ErrorMessage = "Invalid verification token."
            };
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

        if (user == null)
        {
            _logger.LogWarning("Email verification attempted with invalid or already used token");
            return new EmailVerificationResult
            {
                Success = false,
                ErrorMessage = "This verification link is invalid or has already been used.",
                TokenAlreadyUsed = true
            };
        }

        // Check if token has expired
        if (user.EmailVerificationTokenExpiry.HasValue && user.EmailVerificationTokenExpiry.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("Email verification attempted with expired token for user: {Email}", user.Email);
            return new EmailVerificationResult
            {
                Success = false,
                ErrorMessage = "This verification link has expired. Please request a new verification email.",
                TokenExpired = true,
                User = user
            };
        }

        // Verify the email
        user.Status = AccountStatus.Active;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Email verified successfully for user: {Email}", user.Email);

        return new EmailVerificationResult
        {
            Success = true,
            User = user
        };
    }

    /// <inheritdoc />
    public async Task<string?> GenerateNewVerificationTokenAsync(string email)
    {
        var normalizedEmail = email.ToLowerInvariant().Trim();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null)
        {
            _logger.LogWarning("New verification token requested for non-existent email: {Email}", normalizedEmail);
            return null;
        }

        // Only generate new token for unverified accounts
        if (user.Status != AccountStatus.Unverified)
        {
            _logger.LogWarning("New verification token requested for already verified email: {Email}", normalizedEmail);
            return null;
        }

        // Generate new token
        var newToken = GenerateVerificationToken();
        user.EmailVerificationToken = newToken;
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.Add(DefaultTokenExpiryDuration);

        await _context.SaveChangesAsync();

        // Send new verification email
        await _emailService.SendVerificationEmailAsync(user.Email, newToken);

        _logger.LogInformation("New verification token generated and sent to: {Email}", user.Email);

        return newToken;
    }

    private static string GenerateVerificationToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(VerificationTokenSizeBytes));
    }
}
