using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a delivery or billing address.
/// Supports multiple countries and regions where Mercato operates.
/// </summary>
public class Address
{
    /// <summary>
    /// Gets or sets the unique identifier for the address.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID (null for guest checkout).
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the user (navigation property).
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the full name of the recipient.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the phone number for delivery contact.
    /// </summary>
    [Required]
    [Phone]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the first line of the street address.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the second line of the street address (optional).
    /// </summary>
    [MaxLength(200)]
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Gets or sets the city name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state or province name.
    /// </summary>
    [MaxLength(100)]
    public string? StateProvince { get; set; }

    /// <summary>
    /// Gets or sets the postal or ZIP code.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the country code (ISO 3166-1 alpha-2).
    /// Examples: "US", "CA", "GB", "DE", "FR"
    /// </summary>
    [Required]
    [MaxLength(2)]
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is the default address for the user.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets delivery instructions for this address (optional).
    /// Examples: "Leave at front door", "Ring doorbell", "Call upon arrival"
    /// </summary>
    [MaxLength(500)]
    public string? DeliveryInstructions { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the address was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the address was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
