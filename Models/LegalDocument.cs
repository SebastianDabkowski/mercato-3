using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a versioned legal document such as Terms of Service or Privacy Policy.
/// Supports versioning with effective dates and audit trail.
/// </summary>
public class LegalDocument
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the type of legal document.
    /// </summary>
    [Required]
    public LegalDocumentType DocumentType { get; set; }

    /// <summary>
    /// Gets or sets the version number (e.g., 1.0, 1.1, 2.0).
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Version { get; set; } = null!;

    /// <summary>
    /// Gets or sets the document title.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    /// <summary>
    /// Gets or sets the HTML content of the document.
    /// </summary>
    [Required]
    public string Content { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date when this version becomes effective.
    /// Users viewing the document before this date will see the current active version.
    /// </summary>
    [Required]
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Gets or sets whether this version is currently active.
    /// Only one version per document type should be active at any given time.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets when the document version was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the admin user who created this version.
    /// </summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the admin user who created this version.
    /// </summary>
    public User? CreatedByUser { get; set; }

    /// <summary>
    /// Gets or sets when the document version was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the admin user who last updated this version.
    /// </summary>
    public int? UpdatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the admin user who last updated this version.
    /// </summary>
    public User? UpdatedByUser { get; set; }

    /// <summary>
    /// Gets or sets notes about this version (e.g., what changed).
    /// </summary>
    [MaxLength(1000)]
    public string? ChangeNotes { get; set; }

    /// <summary>
    /// Gets or sets the language code for this document (e.g., "en", "es", "fr").
    /// Defaults to "en" for English. Used for future multilingual support.
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string LanguageCode { get; set; } = "en";
}
