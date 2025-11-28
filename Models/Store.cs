using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a seller's store in the marketplace.
/// </summary>
public class Store
{
    /// <summary>
    /// Gets or sets the unique identifier for the store.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the store owner.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the store owner (navigation property).
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the store name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL-friendly slug for the store.
    /// </summary>
    [Required]
    [MaxLength(150)]
    public string Slug { get; set; } = string.Empty;

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

    /// <summary>
    /// Gets or sets the URL or path to the store logo image.
    /// </summary>
    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Gets or sets the store contact email address.
    /// </summary>
    [EmailAddress]
    [MaxLength(256)]
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Gets or sets the store contact phone number.
    /// </summary>
    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the store website URL.
    /// </summary>
    [Url]
    [MaxLength(500)]
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Gets or sets the store status.
    /// </summary>
    public StoreStatus Status { get; set; } = StoreStatus.PendingVerification;

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
    /// Gets or sets the bank account number (encrypted in production).
    /// </summary>
    [MaxLength(100)]
    public string? BankAccountNumber { get; set; }

    /// <summary>
    /// Gets or sets the bank routing number or SWIFT code.
    /// </summary>
    [MaxLength(50)]
    public string? BankRoutingNumber { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the store was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the store was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the status of a store.
/// </summary>
public enum StoreStatus
{
    /// <summary>
    /// Store is created but pending verification.
    /// </summary>
    PendingVerification,

    /// <summary>
    /// Store is active and can list products.
    /// </summary>
    Active,

    /// <summary>
    /// Store is active with limited functionality (e.g., reduced listing quota).
    /// </summary>
    LimitedActive,

    /// <summary>
    /// Store has been suspended.
    /// </summary>
    Suspended
}
