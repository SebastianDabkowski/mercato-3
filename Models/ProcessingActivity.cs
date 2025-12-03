using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a personal data processing activity as per GDPR Article 30.
/// This model maintains the registry of processing activities required for GDPR compliance.
/// </summary>
public class ProcessingActivity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the processing activity.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the purpose(s) of the processing.
    /// </summary>
    [Required]
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the legal basis for processing (e.g., consent, contract, legal obligation).
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string LegalBasis { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the categories of personal data being processed.
    /// </summary>
    [Required]
    public string DataCategories { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the categories of data subjects.
    /// </summary>
    [Required]
    public string DataSubjects { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the categories of recipients to whom data may be disclosed.
    /// </summary>
    public string? Recipients { get; set; }

    /// <summary>
    /// Gets or sets information about transfers to third countries or international organizations.
    /// </summary>
    public string? InternationalTransfers { get; set; }

    /// <summary>
    /// Gets or sets the retention period or criteria for the data.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string RetentionPeriod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the technical and organizational security measures.
    /// </summary>
    public string? SecurityMeasures { get; set; }

    /// <summary>
    /// Gets or sets the name(s) of any data processors involved.
    /// </summary>
    public string? Processors { get; set; }

    /// <summary>
    /// Gets or sets whether the processing is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when the processing activity record was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the ID of the user who created this record.
    /// </summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user who created this record.
    /// </summary>
    public User? CreatedByUser { get; set; }

    /// <summary>
    /// Gets or sets when the processing activity record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who last updated this record.
    /// </summary>
    public int? UpdatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user who last updated this record.
    /// </summary>
    public User? UpdatedByUser { get; set; }

    /// <summary>
    /// Gets or sets notes about this processing activity.
    /// </summary>
    public string? Notes { get; set; }
}
