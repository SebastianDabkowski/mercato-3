using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents the status of a product import job.
/// </summary>
public enum ProductImportJobStatus
{
    /// <summary>
    /// Job has been created but not started yet (preview stage).
    /// </summary>
    Pending,

    /// <summary>
    /// Job is currently being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// Job completed successfully with no errors.
    /// </summary>
    Completed,

    /// <summary>
    /// Job completed but some products failed to import.
    /// </summary>
    CompletedWithErrors,

    /// <summary>
    /// Job failed completely.
    /// </summary>
    Failed,

    /// <summary>
    /// Job was cancelled by the user.
    /// </summary>
    Cancelled
}

/// <summary>
/// Represents a product import job.
/// Tracks the status and statistics of a product catalog import operation.
/// </summary>
public class ProductImportJob
{
    /// <summary>
    /// Gets or sets the unique identifier for this import job.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the store ID that owns this import job.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store that owns this import job (navigation property).
    /// </summary>
    public Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user ID who initiated the import.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the user who initiated the import (navigation property).
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the original filename of the uploaded file.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file type (CSV or Excel).
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the import job.
    /// </summary>
    public ProductImportJobStatus Status { get; set; } = ProductImportJobStatus.Pending;

    /// <summary>
    /// Gets or sets the total number of rows in the import file (excluding header).
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Gets or sets the number of products successfully created.
    /// </summary>
    public int CreatedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of products successfully updated.
    /// </summary>
    public int UpdatedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of products that failed to import.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the job was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the job started processing.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the job completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets error message if the job failed completely.
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the import results for individual rows.
    /// </summary>
    public ICollection<ProductImportResult> Results { get; set; } = new List<ProductImportResult>();
}
