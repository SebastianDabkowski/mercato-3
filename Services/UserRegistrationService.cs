using System.Security.Cryptography;
using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Result of a registration attempt.
/// </summary>
public class RegistrationResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public User? User { get; set; }
}

/// <summary>
/// Data required for user registration.
/// </summary>
public class RegistrationData
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? TaxId { get; set; }
    public required UserType UserType { get; set; }
    public required bool AcceptedTerms { get; set; }
}

/// <summary>
/// Interface for user registration service.
/// </summary>
public interface IUserRegistrationService
{
    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="data">The registration data.</param>
    /// <returns>The registration result.</returns>
    Task<RegistrationResult> RegisterAsync(RegistrationData data);

    /// <summary>
    /// Checks if an email address is already in use.
    /// </summary>
    /// <param name="email">The email to check.</param>
    /// <returns>True if the email is already registered.</returns>
    Task<bool> IsEmailRegisteredAsync(string email);
}

/// <summary>
/// Service for user registration.
/// </summary>
public class UserRegistrationService : IUserRegistrationService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordValidationService _passwordValidation;
    private readonly IEmailService _emailService;

    public UserRegistrationService(
        ApplicationDbContext context,
        IPasswordValidationService passwordValidation,
        IEmailService emailService)
    {
        _context = context;
        _passwordValidation = passwordValidation;
        _emailService = emailService;
    }

    /// <inheritdoc />
    public async Task<RegistrationResult> RegisterAsync(RegistrationData data)
    {
        var result = new RegistrationResult();

        // Validate password
        var passwordValidation = _passwordValidation.Validate(data.Password);
        if (!passwordValidation.IsValid)
        {
            result.Errors.AddRange(passwordValidation.Errors);
        }

        // Check terms accepted
        if (!data.AcceptedTerms)
        {
            result.Errors.Add("You must accept the terms and conditions.");
        }

        // Check email uniqueness
        if (await IsEmailRegisteredAsync(data.Email))
        {
            result.Errors.Add("An account with this email address already exists.");
        }

        if (result.Errors.Count > 0)
        {
            return result;
        }

        // Create user
        var verificationToken = GenerateVerificationToken();
        var user = new User
        {
            Email = data.Email.ToLowerInvariant().Trim(),
            PasswordHash = HashPassword(data.Password),
            FirstName = data.FirstName.Trim(),
            LastName = data.LastName.Trim(),
            PhoneNumber = data.PhoneNumber?.Trim(),
            Address = data.Address?.Trim(),
            City = data.City?.Trim(),
            PostalCode = data.PostalCode?.Trim(),
            Country = data.Country?.Trim(),
            TaxId = data.TaxId?.Trim(),
            UserType = data.UserType,
            AcceptedTerms = true,
            Status = AccountStatus.Unverified,
            CreatedAt = DateTime.UtcNow,
            EmailVerificationToken = verificationToken
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Send verification email
        await _emailService.SendVerificationEmailAsync(user.Email, verificationToken);

        result.Success = true;
        result.User = user;
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> IsEmailRegisteredAsync(string email)
    {
        var normalizedEmail = email.ToLowerInvariant().Trim();
        return await _context.Users.AnyAsync(u => u.Email == normalizedEmail);
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(128 / 8);
        var hashed = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8);

        // Combine salt and hash for storage
        var hashBytes = new byte[salt.Length + hashed.Length];
        Array.Copy(salt, 0, hashBytes, 0, salt.Length);
        Array.Copy(hashed, 0, hashBytes, salt.Length, hashed.Length);

        return Convert.ToBase64String(hashBytes);
    }

    private static string GenerateVerificationToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}
