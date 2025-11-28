using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a seller's verification data submitted for KYC review.
/// Stores different fields based on seller type (company or individual).
/// </summary>
public class SellerVerification
{
    /// <summary>
    /// Gets or sets the unique identifier for the verification.
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
    /// Gets or sets the seller type (Company or Individual).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string SellerType { get; set; } = string.Empty;

    // Company-specific fields

    /// <summary>
    /// Gets or sets the company name (required for company sellers).
    /// </summary>
    [MaxLength(200)]
    public string? CompanyName { get; set; }

    /// <summary>
    /// Gets or sets the company registration number (required for company sellers).
    /// </summary>
    [MaxLength(50)]
    public string? RegistrationNumber { get; set; }

    /// <summary>
    /// Gets or sets the tax identification number (required for company sellers).
    /// </summary>
    [MaxLength(50)]
    public string? TaxId { get; set; }

    /// <summary>
    /// Gets or sets the registered address (required for company sellers).
    /// </summary>
    [MaxLength(500)]
    public string? RegisteredAddress { get; set; }

    /// <summary>
    /// Gets or sets the contact person name (required for company sellers).
    /// </summary>
    [MaxLength(200)]
    public string? ContactPersonName { get; set; }

    /// <summary>
    /// Gets or sets the contact person email (required for company sellers).
    /// </summary>
    [MaxLength(256)]
    [EmailAddress]
    public string? ContactPersonEmail { get; set; }

    /// <summary>
    /// Gets or sets the contact person phone number (required for company sellers).
    /// </summary>
    [MaxLength(20)]
    [Phone]
    public string? ContactPersonPhone { get; set; }

    // Individual-specific fields

    /// <summary>
    /// Gets or sets the full name (required for individual sellers).
    /// </summary>
    [MaxLength(200)]
    public string? FullName { get; set; }

    /// <summary>
    /// Gets or sets the personal ID number or equivalent (required for individual sellers).
    /// </summary>
    [MaxLength(50)]
    public string? PersonalIdNumber { get; set; }

    /// <summary>
    /// Gets or sets the address (required for individual sellers).
    /// </summary>
    [MaxLength(500)]
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the contact email (required for individual sellers).
    /// </summary>
    [MaxLength(256)]
    [EmailAddress]
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Gets or sets the contact phone number (required for individual sellers).
    /// </summary>
    [MaxLength(20)]
    [Phone]
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the verification was submitted.
    /// </summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the verification was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
