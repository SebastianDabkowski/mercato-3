using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a seller's onboarding wizard draft data.
/// This stores the data entered by the seller during the onboarding process.
/// </summary>
public class SellerOnboardingDraft
{
    /// <summary>
    /// Gets or sets the unique identifier for the draft.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the seller.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the seller user (navigation property).
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the current step of the onboarding wizard (1-based).
    /// </summary>
    public int CurrentStep { get; set; } = 1;

    /// <summary>
    /// Gets or sets the last completed step (0 means no step completed).
    /// </summary>
    public int LastCompletedStep { get; set; } = 0;

    // Step 1: Store Profile Basics

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    [MaxLength(100)]
    public string? StoreName { get; set; }

    /// <summary>
    /// Gets or sets the store description.
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the store category.
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    // Step 2: Verification Data

    /// <summary>
    /// Gets or sets the business type (Individual or Business).
    /// </summary>
    [MaxLength(50)]
    public string? BusinessType { get; set; }

    /// <summary>
    /// Gets or sets the business registration number.
    /// </summary>
    [MaxLength(50)]
    public string? BusinessRegistrationNumber { get; set; }

    /// <summary>
    /// Gets or sets the tax identification number.
    /// </summary>
    [MaxLength(50)]
    public string? TaxId { get; set; }

    // Step 3: Payout Basics

    /// <summary>
    /// Gets or sets the bank name for payouts.
    /// </summary>
    [MaxLength(100)]
    public string? BankName { get; set; }

    /// <summary>
    /// Gets or sets the bank account holder name.
    /// </summary>
    [MaxLength(100)]
    public string? BankAccountHolderName { get; set; }

    /// <summary>
    /// Gets or sets the bank account number.
    /// </summary>
    [MaxLength(100)]
    public string? BankAccountNumber { get; set; }

    /// <summary>
    /// Gets or sets the bank routing number or SWIFT code.
    /// </summary>
    [MaxLength(50)]
    public string? BankRoutingNumber { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the draft was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the draft was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
