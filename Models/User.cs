using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a registered user in the marketplace.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user's email address (unique across all users).
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hashed password.
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's phone number for contact purposes.
    /// </summary>
    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the user's address line.
    /// </summary>
    [MaxLength(200)]
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the user's city.
    /// </summary>
    [MaxLength(100)]
    public string? City { get; set; }

    /// <summary>
    /// Gets or sets the user's postal code.
    /// </summary>
    [MaxLength(20)]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Gets or sets the user's country.
    /// </summary>
    [MaxLength(100)]
    public string? Country { get; set; }

    /// <summary>
    /// Gets or sets the tax identification number (for sellers and KYC).
    /// </summary>
    [MaxLength(50)]
    public string? TaxId { get; set; }

    /// <summary>
    /// Gets or sets the type of user account (Buyer or Seller).
    /// </summary>
    public UserType UserType { get; set; }

    /// <summary>
    /// Gets or sets the account status.
    /// </summary>
    public AccountStatus Status { get; set; } = AccountStatus.Unverified;

    /// <summary>
    /// Gets or sets whether the user has accepted the terms and conditions.
    /// </summary>
    public bool AcceptedTerms { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the email verification token.
    /// </summary>
    [MaxLength(256)]
    public string? EmailVerificationToken { get; set; }

    /// <summary>
    /// Gets or sets the expiry time for the email verification token.
    /// </summary>
    public DateTime? EmailVerificationTokenExpiry { get; set; }

    /// <summary>
    /// Gets or sets the external login provider (e.g., "Google", "Facebook").
    /// Null for users who registered with email/password.
    /// </summary>
    [MaxLength(50)]
    public string? ExternalProvider { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier from the external provider.
    /// </summary>
    [MaxLength(256)]
    public string? ExternalProviderId { get; set; }

    /// <summary>
    /// Gets or sets the KYC (Know Your Customer) verification status.
    /// Only applicable for sellers.
    /// </summary>
    public KycStatus KycStatus { get; set; } = KycStatus.NotStarted;

    /// <summary>
    /// Gets or sets the date and time when KYC was submitted.
    /// </summary>
    public DateTime? KycSubmittedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when KYC was approved or rejected.
    /// </summary>
    public DateTime? KycCompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the password reset token.
    /// </summary>
    [MaxLength(256)]
    public string? PasswordResetToken { get; set; }

    /// <summary>
    /// Gets or sets the expiry time for the password reset token.
    /// </summary>
    public DateTime? PasswordResetTokenExpiry { get; set; }

    /// <summary>
    /// Gets or sets the security stamp (changes when password is updated to invalidate sessions).
    /// </summary>
    [MaxLength(256)]
    public string? SecurityStamp { get; set; }

    // 2FA Configuration Properties

    /// <summary>
    /// Gets or sets whether two-factor authentication is enabled for this account.
    /// </summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// Gets or sets the encrypted secret key used for TOTP-based 2FA.
    /// This should be encrypted at rest in production environments.
    /// </summary>
    [MaxLength(512)]
    public string? TwoFactorSecretKey { get; set; }

    /// <summary>
    /// Gets or sets the recovery codes for 2FA (comma-separated, hashed).
    /// Used when the user loses access to their authenticator app.
    /// </summary>
    [MaxLength(1024)]
    public string? TwoFactorRecoveryCodes { get; set; }

    /// <summary>
    /// Gets or sets the date and time when 2FA was enabled.
    /// </summary>
    public DateTime? TwoFactorEnabledAt { get; set; }

    // Account Blocking Properties

    /// <summary>
    /// Gets or sets the ID of the admin user who blocked this account.
    /// </summary>
    public int? BlockedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the account was blocked.
    /// </summary>
    public DateTime? BlockedAt { get; set; }

    /// <summary>
    /// Gets or sets the reason why the account was blocked.
    /// </summary>
    public BlockReason? BlockReason { get; set; }

    /// <summary>
    /// Gets or sets additional notes about why the account was blocked.
    /// </summary>
    [MaxLength(1000)]
    public string? BlockNotes { get; set; }
}
