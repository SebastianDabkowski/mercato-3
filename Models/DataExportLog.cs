using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a log entry for user data export requests.
/// Used for GDPR compliance and audit trail.
/// </summary>
public class DataExportLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the export log entry.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID who requested the data export.
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the user who requested the export.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets when the export was requested.
    /// </summary>
    [Required]
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the export was completed (null if not yet completed).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the export was requested.
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent string from which the export was requested.
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the file path of the exported data (if stored on server).
    /// </summary>
    [MaxLength(500)]
    public string? FilePath { get; set; }

    /// <summary>
    /// Gets or sets the size of the exported file in bytes.
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets any error message if the export failed.
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the format of the export (e.g., "JSON", "CSV").
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Format { get; set; } = "JSON";

    /// <summary>
    /// Gets or sets whether the export was successful.
    /// </summary>
    public bool IsSuccessful { get; set; }
}
