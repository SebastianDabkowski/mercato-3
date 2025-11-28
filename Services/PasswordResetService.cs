using System.Security.Cryptography;
using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Result of a password reset request.
/// </summary>
public class PasswordResetRequestResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of a password reset token validation.
/// </summary>
public class PasswordResetTokenValidationResult
{
    public bool IsValid { get; set; }
    public bool TokenExpired { get; set; }
    public bool TokenNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public User? User { get; set; }
}

/// <summary>
/// Result of a password reset operation.
/// </summary>
public class PasswordResetResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool TokenExpired { get; set; }
    public bool TokenInvalid { get; set; }
}

/// <summary>
/// Result of a password change operation.
/// </summary>
public class PasswordChangeResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Interface for password reset service.
/// </summary>
public interface IPasswordResetService
{
    /// <summary>
    /// Requests a password reset for the specified email.
    /// Always returns success to prevent email enumeration.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <returns>The result of the request.</returns>
    Task<PasswordResetRequestResult> RequestPasswordResetAsync(string email);

    /// <summary>
    /// Validates a password reset token.
    /// </summary>
    /// <param name="token">The reset token.</param>
    /// <returns>The validation result.</returns>
    Task<PasswordResetTokenValidationResult> ValidateResetTokenAsync(string token);

    /// <summary>
    /// Resets the password using a valid reset token.
    /// </summary>
    /// <param name="token">The reset token.</param>
    /// <param name="newPassword">The new password.</param>
    /// <returns>The result of the password reset.</returns>
    Task<PasswordResetResult> ResetPasswordAsync(string token, string newPassword);

    /// <summary>
    /// Changes the password for an authenticated user.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="currentPassword">The current password.</param>
    /// <param name="newPassword">The new password.</param>
    /// <returns>The result of the password change.</returns>
    Task<PasswordChangeResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

    /// <summary>
    /// Gets the reset token expiry duration.
    /// </summary>
    TimeSpan TokenExpiryDuration { get; }
}

/// <summary>
/// Service for handling password reset and change operations.
/// </summary>
public class PasswordResetService : IPasswordResetService
{
    // Password hashing constants (must match UserRegistrationService and UserAuthenticationService)
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int Pbkdf2Iterations = 100000;
    private const int ResetTokenSizeBytes = 32;
    private static readonly TimeSpan DefaultTokenExpiryDuration = TimeSpan.FromHours(1);

    // Marker for social-only accounts (must match SocialLoginService and UserAuthenticationService)
    private const string SocialLoginNoPasswordMarker = "SOCIAL_LOGIN_NO_PASSWORD";

    private readonly ApplicationDbContext _context;
    private readonly IPasswordValidationService _passwordValidation;
    private readonly IEmailService _emailService;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        ApplicationDbContext context,
        IPasswordValidationService passwordValidation,
        IEmailService emailService,
        ILogger<PasswordResetService> logger)
    {
        _context = context;
        _passwordValidation = passwordValidation;
        _emailService = emailService;
        _logger = logger;
    }

    /// <inheritdoc />
    public TimeSpan TokenExpiryDuration => DefaultTokenExpiryDuration;

    /// <inheritdoc />
    public async Task<PasswordResetRequestResult> RequestPasswordResetAsync(string email)
    {
        var normalizedEmail = email.ToLowerInvariant().Trim();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        // Always log the request but don't reveal whether the email exists
        _logger.LogInformation("Password reset requested for email: {Email}", normalizedEmail);

        if (user != null)
        {
            // Don't allow password reset for social login only accounts
            if (user.PasswordHash == SocialLoginNoPasswordMarker)
            {
                _logger.LogWarning("Password reset requested for social-only account: {Email}", normalizedEmail);
                // Still return success to prevent enumeration
                return new PasswordResetRequestResult { Success = true };
            }

            // Generate reset token
            var resetToken = GenerateResetToken();
            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.Add(DefaultTokenExpiryDuration);

            await _context.SaveChangesAsync();

            // Send password reset email
            await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken);

            _logger.LogInformation("Password reset token generated for: {Email}", normalizedEmail);
        }

        // Always return success to prevent email enumeration
        return new PasswordResetRequestResult { Success = true };
    }

    /// <inheritdoc />
    public async Task<PasswordResetTokenValidationResult> ValidateResetTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new PasswordResetTokenValidationResult
            {
                IsValid = false,
                TokenNotFound = true,
                ErrorMessage = "Invalid password reset link."
            };
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token);

        if (user == null)
        {
            _logger.LogWarning("Password reset attempted with invalid or used token");
            return new PasswordResetTokenValidationResult
            {
                IsValid = false,
                TokenNotFound = true,
                ErrorMessage = "This password reset link is invalid or has already been used."
            };
        }

        // Check if token has expired
        if (user.PasswordResetTokenExpiry.HasValue && user.PasswordResetTokenExpiry.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("Password reset attempted with expired token for user: {Email}", user.Email);
            return new PasswordResetTokenValidationResult
            {
                IsValid = false,
                TokenExpired = true,
                ErrorMessage = "This password reset link has expired. Please request a new one.",
                User = user
            };
        }

        return new PasswordResetTokenValidationResult
        {
            IsValid = true,
            User = user
        };
    }

    /// <inheritdoc />
    public async Task<PasswordResetResult> ResetPasswordAsync(string token, string newPassword)
    {
        var result = new PasswordResetResult();

        // Validate the token
        var tokenValidation = await ValidateResetTokenAsync(token);
        if (!tokenValidation.IsValid)
        {
            result.TokenExpired = tokenValidation.TokenExpired;
            result.TokenInvalid = tokenValidation.TokenNotFound;
            result.Errors.Add(tokenValidation.ErrorMessage ?? "Invalid token.");
            return result;
        }

        var user = tokenValidation.User!;

        // Validate the new password
        var passwordValidation = _passwordValidation.Validate(newPassword);
        if (!passwordValidation.IsValid)
        {
            result.Errors.AddRange(passwordValidation.Errors);
            return result;
        }

        // Update the password
        user.PasswordHash = HashPassword(newPassword);
        
        // Clear the reset token (single-use)
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        // Update security stamp to invalidate all active sessions
        user.SecurityStamp = GenerateSecurityStamp();

        await _context.SaveChangesAsync();

        _logger.LogInformation("Password reset successfully for user: {Email}", user.Email);

        result.Success = true;
        return result;
    }

    /// <inheritdoc />
    public async Task<PasswordChangeResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var result = new PasswordChangeResult();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            result.Errors.Add("User not found.");
            return result;
        }

        // Check if this is a social login only account
        if (user.PasswordHash == SocialLoginNoPasswordMarker)
        {
            _logger.LogWarning("Password change attempted for social-only account: {Email}", user.Email);
            result.Errors.Add("This account uses social login. Password cannot be changed.");
            return result;
        }

        // Verify current password
        if (!VerifyPassword(currentPassword, user.PasswordHash))
        {
            result.Errors.Add("Current password is incorrect.");
            return result;
        }

        // Validate the new password
        var passwordValidation = _passwordValidation.Validate(newPassword);
        if (!passwordValidation.IsValid)
        {
            result.Errors.AddRange(passwordValidation.Errors);
            return result;
        }

        // Update the password
        user.PasswordHash = HashPassword(newPassword);

        // Update security stamp to invalidate other sessions
        user.SecurityStamp = GenerateSecurityStamp();

        await _context.SaveChangesAsync();

        _logger.LogInformation("Password changed successfully for user: {Email}", user.Email);

        result.Success = true;
        return result;
    }

    private static string GenerateResetToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(ResetTokenSizeBytes));
    }

    private static string GenerateSecurityStamp()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hashed = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: Pbkdf2Iterations,
            numBytesRequested: HashSizeBytes);

        // Combine salt and hash for storage
        var hashBytes = new byte[salt.Length + hashed.Length];
        Array.Copy(salt, 0, hashBytes, 0, salt.Length);
        Array.Copy(hashed, 0, hashBytes, salt.Length, hashed.Length);

        return Convert.ToBase64String(hashBytes);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            var hashBytes = Convert.FromBase64String(storedHash);

            // Extract salt from stored hash
            if (hashBytes.Length != SaltSizeBytes + HashSizeBytes)
            {
                return false;
            }

            var salt = new byte[SaltSizeBytes];
            var storedHashValue = new byte[HashSizeBytes];
            Array.Copy(hashBytes, 0, salt, 0, SaltSizeBytes);
            Array.Copy(hashBytes, SaltSizeBytes, storedHashValue, 0, HashSizeBytes);

            // Hash the provided password with the same salt
            var computedHash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: Pbkdf2Iterations,
                numBytesRequested: HashSizeBytes);

            // Use constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(computedHash, storedHashValue);
        }
        catch
        {
            return false;
        }
    }
}
