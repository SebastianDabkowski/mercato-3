using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a historical version of a processing activity for audit trail and versioning.
/// Captures who changed what and when for GDPR compliance audits.
/// </summary>
public class ProcessingActivityHistory
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the processing activity this history entry belongs to.
    /// </summary>
    [Required]
    public int ProcessingActivityId { get; set; }

    /// <summary>
    /// Gets or sets the processing activity this history entry belongs to.
    /// </summary>
    public ProcessingActivity? ProcessingActivity { get; set; }

    /// <summary>
    /// Gets or sets the name of the processing activity at this point in time.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the purpose(s) of the processing at this point in time.
    /// </summary>
    [Required]
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the legal basis at this point in time.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string LegalBasis { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data categories at this point in time.
    /// </summary>
    [Required]
    public string DataCategories { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data subjects at this point in time.
    /// </summary>
    [Required]
    public string DataSubjects { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recipients at this point in time.
    /// </summary>
    public string? Recipients { get; set; }

    /// <summary>
    /// Gets or sets the international transfers at this point in time.
    /// </summary>
    public string? InternationalTransfers { get; set; }

    /// <summary>
    /// Gets or sets the retention period at this point in time.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string RetentionPeriod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the security measures at this point in time.
    /// </summary>
    public string? SecurityMeasures { get; set; }

    /// <summary>
    /// Gets or sets the processors at this point in time.
    /// </summary>
    public string? Processors { get; set; }

    /// <summary>
    /// Gets or sets whether the processing was active at this point in time.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the notes at this point in time.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets when this version was created (timestamp of the change).
    /// </summary>
    [Required]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the ID of the user who made this change.
    /// </summary>
    public int? ChangedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user who made this change.
    /// </summary>
    public User? ChangedByUser { get; set; }

    /// <summary>
    /// Gets or sets notes about what was changed.
    /// </summary>
    [MaxLength(1000)]
    public string? ChangeNotes { get; set; }
}
