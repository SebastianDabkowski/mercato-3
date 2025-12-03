using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a user's acceptance or withdrawal of a specific consent type.
/// Used for GDPR compliance and audit purposes.
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
    /// Gets or sets the type of consent.
    /// </summary>
    [Required]
    public ConsentType ConsentType { get; set; }

    /// <summary>
    /// Gets or sets the legal document ID that was accepted (optional for non-document consents).
    /// </summary>
    public int? LegalDocumentId { get; set; }

    /// <summary>
    /// Gets or sets the legal document that was accepted (optional).
    /// </summary>
    public LegalDocument? LegalDocument { get; set; }

    /// <summary>
    /// Gets or sets the version of the consent text when it was granted.
    /// Stored separately from LegalDocument to track consent for non-document types.
    /// </summary>
    [MaxLength(20)]
    public string? ConsentVersion { get; set; }

    /// <summary>
    /// Gets or sets the consent text that was presented to the user.
    /// Stored for audit trail.
    /// </summary>
    [MaxLength(2000)]
    public string? ConsentText { get; set; }

    /// <summary>
    /// Gets or sets whether consent is granted (true) or withdrawn (false).
    /// </summary>
    [Required]
    public bool IsGranted { get; set; }

    /// <summary>
    /// Gets or sets when the consent was given or withdrawn.
    /// </summary>
    [Required]
    public DateTime ConsentedAt { get; set; }

    /// <summary>
    /// Gets or sets when this consent record was superseded by a newer version.
    /// Null if this is the current active consent record for the user and type.
    /// </summary>
    public DateTime? SupersededAt { get; set; }

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
    /// Gets or sets the context in which consent was given (e.g., "registration", "checkout", "privacy_settings").
    /// </summary>
    [MaxLength(50)]
    public string? ConsentContext { get; set; }
}
