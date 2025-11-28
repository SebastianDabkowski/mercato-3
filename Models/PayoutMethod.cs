using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a payout method configured by a seller.
/// </summary>
public class PayoutMethod
{
    /// <summary>
    /// Gets or sets the unique identifier for the payout method.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the store ID this payout method belongs to.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store this payout method belongs to (navigation property).
    /// </summary>
    public Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of payout method.
    /// </summary>
    public PayoutMethodType MethodType { get; set; }

    /// <summary>
    /// Gets or sets the display name for this payout method (e.g., "Main Bank Account").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bank name (for bank transfer methods).
    /// </summary>
    [MaxLength(100)]
    public string? BankName { get; set; }

    /// <summary>
    /// Gets or sets the bank account holder name.
    /// </summary>
    [MaxLength(100)]
    public string? BankAccountHolderName { get; set; }

    /// <summary>
    /// Gets or sets the encrypted bank account number.
    /// In production, this should be encrypted at rest.
    /// </summary>
    [MaxLength(256)]
    public string? BankAccountNumberEncrypted { get; set; }

    /// <summary>
    /// Gets or sets the last 4 digits of the bank account number for display purposes.
    /// </summary>
    [MaxLength(4)]
    public string? BankAccountNumberLast4 { get; set; }

    /// <summary>
    /// Gets or sets the bank routing number, SWIFT/BIC code, or sort code.
    /// </summary>
    [MaxLength(50)]
    public string? BankRoutingNumber { get; set; }

    /// <summary>
    /// Gets or sets the currency for this payout method (ISO 4217 code).
    /// </summary>
    [MaxLength(3)]
    public string? Currency { get; set; }

    /// <summary>
    /// Gets or sets the country code (ISO 3166-1 alpha-2) for this payout method.
    /// </summary>
    [MaxLength(2)]
    public string? CountryCode { get; set; }

    /// <summary>
    /// Gets or sets whether this payout method is the default for the store.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets whether this payout method has been verified.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Gets or sets the verification status of this payout method.
    /// </summary>
    public PayoutMethodVerificationStatus VerificationStatus { get; set; } = PayoutMethodVerificationStatus.Pending;

    /// <summary>
    /// Gets or sets the date and time when the payout method was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the payout method was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the type of payout method.
/// </summary>
public enum PayoutMethodType
{
    /// <summary>
    /// Bank transfer (ACH, wire, SEPA, etc.).
    /// </summary>
    BankTransfer,

    /// <summary>
    /// Payment account (e.g., PayPal, Stripe Connect).
    /// </summary>
    PaymentAccount
}

/// <summary>
/// Represents the verification status of a payout method.
/// </summary>
public enum PayoutMethodVerificationStatus
{
    /// <summary>
    /// Verification is pending.
    /// </summary>
    Pending,

    /// <summary>
    /// Verification is in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Payout method has been verified.
    /// </summary>
    Verified,

    /// <summary>
    /// Verification failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Payout method requires re-verification.
    /// </summary>
    RequiresReVerification
}
