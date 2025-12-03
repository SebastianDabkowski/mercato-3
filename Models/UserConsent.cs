using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a user's acceptance of a specific version of a legal document.
/// Used for compliance and audit purposes.
/// </summary>
public class UserConsent
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID who gave consent.
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the user who gave consent.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the legal document ID that was accepted.
    /// </summary>
    [Required]
    public int LegalDocumentId { get; set; }

    /// <summary>
    /// Gets or sets the legal document that was accepted.
    /// </summary>
    public LegalDocument LegalDocument { get; set; } = null!;

    /// <summary>
    /// Gets or sets when the consent was given.
    /// </summary>
    [Required]
    public DateTime ConsentedAt { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which consent was given.
    /// Stored for audit purposes.
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent string from which consent was given.
    /// Stored for audit purposes.
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the context in which consent was given (e.g., "registration", "checkout").
    /// </summary>
    [MaxLength(50)]
    public string? ConsentContext { get; set; }
}
