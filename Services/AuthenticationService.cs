using System.Collections.Concurrent;
using System.Security.Cryptography;
using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Result of a login attempt.
/// </summary>
public class LoginResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public User? User { get; set; }
    public bool RequiresEmailVerification { get; set; }
    public bool RequiresKyc { get; set; }
}

/// <summary>
/// Data required for user login.
/// </summary>
public class LoginData
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

/// <summary>
/// Interface for user authentication service.
/// </summary>
public interface IUserAuthenticationService
{
    /// <summary>
    /// Attempts to authenticate a user with email and password.
    /// </summary>
    /// <param name="data">The login data.</param>
    /// <returns>The login result.</returns>
    Task<LoginResult> AuthenticateAsync(LoginData data);
}

/// <summary>
/// Service for user authentication with rate limiting.
/// Note: Rate limiting uses in-memory storage suitable for single-instance deployments.
/// For multi-instance production environments, replace with distributed cache (e.g., Redis).
/// </summary>
public class UserAuthenticationService : IUserAuthenticationService
{
    // Password hashing constants (must match UserRegistrationService)
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int Pbkdf2Iterations = 100000;

    // Rate limiting constants
    private const int MaxLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan AttemptWindowDuration = TimeSpan.FromMinutes(15);

    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserAuthenticationService> _logger;

    // Thread-safe dictionary to track login attempts per email
    private static readonly ConcurrentDictionary<string, LoginAttemptInfo> LoginAttempts = new();

    public UserAuthenticationService(
        ApplicationDbContext context,
        ILogger<UserAuthenticationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<LoginResult> AuthenticateAsync(LoginData data)
    {
        var normalizedEmail = data.Email.ToLowerInvariant().Trim();

        // Check rate limiting
        if (IsLockedOut(normalizedEmail))
        {
            _logger.LogWarning("Login attempt blocked due to rate limiting for email: {Email}", normalizedEmail);
            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Too many failed login attempts. Please try again later."
            };
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        // Check if this is a social-only account (cannot login with password)
        if (user != null && user.PasswordHash == "SOCIAL_LOGIN_NO_PASSWORD")
        {
            _logger.LogWarning("Password login attempted for social-only account: {Email}", normalizedEmail);
            return new LoginResult
            {
                Success = false,
                ErrorMessage = "This account uses social login. Please use Google or Facebook to sign in."
            };
        }

        // Use generic error message to prevent user enumeration
        if (user == null || !VerifyPassword(data.Password, user.PasswordHash))
        {
            RecordFailedAttempt(normalizedEmail);
            _logger.LogWarning("Failed login attempt for email: {Email}", normalizedEmail);
            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Invalid email or password."
            };
        }

        // Check if seller has unverified email
        // Note: Only sellers require email verification before login per business requirements.
        // Buyers can use the platform with an unverified email (common for marketplace platforms).
        if (user.UserType == UserType.Seller && user.Status == AccountStatus.Unverified)
        {
            _logger.LogInformation("Login blocked for unverified seller: {Email}", normalizedEmail);
            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Please verify your email address before logging in.",
                RequiresEmailVerification = true,
                User = user
            };
        }

        // Check if account is suspended
        if (user.Status == AccountStatus.Suspended)
        {
            _logger.LogWarning("Login attempt for suspended account: {Email}", normalizedEmail);
            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Your account has been suspended. Please contact support."
            };
        }

        // Clear failed attempts on successful login
        ClearFailedAttempts(normalizedEmail);
        
        _logger.LogInformation("Successful login for user: {Email}", normalizedEmail);
        return new LoginResult
        {
            Success = true,
            User = user
        };
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

    private static bool IsLockedOut(string email)
    {
        if (!LoginAttempts.TryGetValue(email, out var attemptInfo))
        {
            return false;
        }

        // Clean up old attempts
        attemptInfo.CleanupOldAttempts(AttemptWindowDuration);

        // Check if locked out
        if (attemptInfo.LockedUntil.HasValue && attemptInfo.LockedUntil.Value > DateTime.UtcNow)
        {
            return true;
        }

        // Reset lockout if expired
        if (attemptInfo.LockedUntil.HasValue && attemptInfo.LockedUntil.Value <= DateTime.UtcNow)
        {
            attemptInfo.LockedUntil = null;
            attemptInfo.AttemptTimes.Clear();
        }

        return false;
    }

    private static void RecordFailedAttempt(string email)
    {
        var attemptInfo = LoginAttempts.GetOrAdd(email, _ => new LoginAttemptInfo());
        attemptInfo.CleanupOldAttempts(AttemptWindowDuration);
        attemptInfo.AttemptTimes.Add(DateTime.UtcNow);

        if (attemptInfo.AttemptTimes.Count >= MaxLoginAttempts)
        {
            attemptInfo.LockedUntil = DateTime.UtcNow.Add(LockoutDuration);
        }
    }

    private static void ClearFailedAttempts(string email)
    {
        LoginAttempts.TryRemove(email, out _);
    }

    private class LoginAttemptInfo
    {
        public List<DateTime> AttemptTimes { get; } = new();
        public DateTime? LockedUntil { get; set; }

        public void CleanupOldAttempts(TimeSpan windowDuration)
        {
            var cutoff = DateTime.UtcNow.Subtract(windowDuration);
            AttemptTimes.RemoveAll(t => t < cutoff);
        }
    }
}
