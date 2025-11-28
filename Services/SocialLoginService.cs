using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Represents the result of a social login attempt.
/// </summary>
public class SocialLoginResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public User? User { get; set; }
    public bool IsNewUser { get; set; }
}

/// <summary>
/// Represents user information from an external OAuth provider.
/// </summary>
public class ExternalUserInfo
{
    public required string Provider { get; set; }
    public required string ProviderId { get; set; }
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

/// <summary>
/// Interface for social login service that handles OAuth authentication for buyers.
/// </summary>
public interface ISocialLoginService
{
    /// <summary>
    /// Authenticates or registers a buyer using external OAuth provider information.
    /// </summary>
    /// <param name="externalInfo">The user information from the external provider.</param>
    /// <returns>The social login result.</returns>
    Task<SocialLoginResult> AuthenticateOrRegisterBuyerAsync(ExternalUserInfo externalInfo);
}

/// <summary>
/// Service for handling social login authentication for buyers.
/// This abstraction supports multiple OAuth providers (Google, Facebook) 
/// and is designed to easily extend for future providers (e.g., Apple).
/// </summary>
public class SocialLoginService : ISocialLoginService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SocialLoginService> _logger;

    public SocialLoginService(
        ApplicationDbContext context,
        ILogger<SocialLoginService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SocialLoginResult> AuthenticateOrRegisterBuyerAsync(ExternalUserInfo externalInfo)
    {
        var normalizedEmail = externalInfo.Email.ToLowerInvariant().Trim();

        // First, try to find user by external provider and ID
        var user = await _context.Users.FirstOrDefaultAsync(
            u => u.ExternalProvider == externalInfo.Provider && 
                 u.ExternalProviderId == externalInfo.ProviderId);

        if (user != null)
        {
            // User exists with this external provider - verify it's a buyer
            if (user.UserType != UserType.Buyer)
            {
                _logger.LogWarning(
                    "Social login attempted for non-buyer account: {Email}, Provider: {Provider}",
                    normalizedEmail,
                    externalInfo.Provider);
                
                return new SocialLoginResult
                {
                    Success = false,
                    ErrorMessage = "Social login is only available for buyer accounts."
                };
            }

            // Check if account is suspended
            if (user.Status == AccountStatus.Suspended)
            {
                _logger.LogWarning(
                    "Social login attempt for suspended account: {Email}",
                    normalizedEmail);
                
                return new SocialLoginResult
                {
                    Success = false,
                    ErrorMessage = "Your account has been suspended. Please contact support."
                };
            }

            _logger.LogInformation(
                "Successful social login for existing user: {Email}, Provider: {Provider}",
                normalizedEmail,
                externalInfo.Provider);

            return new SocialLoginResult
            {
                Success = true,
                User = user,
                IsNewUser = false
            };
        }

        // Check if email already exists as a buyer account
        var existingUserByEmail = await _context.Users.FirstOrDefaultAsync(
            u => u.Email == normalizedEmail);

        if (existingUserByEmail != null)
        {
            // Email exists - check if it's a buyer account
            if (existingUserByEmail.UserType != UserType.Buyer)
            {
                _logger.LogWarning(
                    "Social login attempted for email used by non-buyer: {Email}",
                    normalizedEmail);
                
                return new SocialLoginResult
                {
                    Success = false,
                    ErrorMessage = "This email is associated with a seller account. Please use email and password to log in."
                };
            }

            // Check if account is suspended
            if (existingUserByEmail.Status == AccountStatus.Suspended)
            {
                _logger.LogWarning(
                    "Social login attempt for suspended account: {Email}",
                    normalizedEmail);
                
                return new SocialLoginResult
                {
                    Success = false,
                    ErrorMessage = "Your account has been suspended. Please contact support."
                };
            }

            // Link the external provider to existing buyer account
            existingUserByEmail.ExternalProvider = externalInfo.Provider;
            existingUserByEmail.ExternalProviderId = externalInfo.ProviderId;
            
            // Mark email as verified since the provider has verified it
            if (existingUserByEmail.Status == AccountStatus.Unverified)
            {
                existingUserByEmail.Status = AccountStatus.Active;
                existingUserByEmail.EmailVerificationToken = null;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Linked external provider to existing buyer account: {Email}, Provider: {Provider}",
                normalizedEmail,
                externalInfo.Provider);

            return new SocialLoginResult
            {
                Success = true,
                User = existingUserByEmail,
                IsNewUser = false
            };
        }

        // Create a new buyer account
        var newUser = new User
        {
            Email = normalizedEmail,
            PasswordHash = "SOCIAL_LOGIN_NO_PASSWORD", // Marker for social-only accounts - no password login allowed
            FirstName = externalInfo.FirstName?.Trim() ?? "User",
            LastName = externalInfo.LastName?.Trim() ?? string.Empty,
            UserType = UserType.Buyer,
            Status = AccountStatus.Active, // Email is verified by the provider
            AcceptedTerms = true, // Users accept terms via OAuth provider consent screen
            CreatedAt = DateTime.UtcNow,
            ExternalProvider = externalInfo.Provider,
            ExternalProviderId = externalInfo.ProviderId
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created new buyer account via social login: {Email}, Provider: {Provider}",
            normalizedEmail,
            externalInfo.Provider);

        return new SocialLoginResult
        {
            Success = true,
            User = newUser,
            IsNewUser = true
        };
    }
}
