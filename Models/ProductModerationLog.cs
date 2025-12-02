using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents an audit log entry for a product moderation action.
/// </summary>
public class ProductModerationLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the log entry.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the product ID that was moderated.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product that was moderated (navigation property).
    /// </summary>
    public Product Product { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of moderation action taken.
    /// </summary>
    [Required]
    public ProductModerationAction Action { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID who took the action (null for automated actions).
    /// </summary>
    public int? ModeratedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the admin who took the action (navigation property).
    /// </summary>
    public User? ModeratedByUser { get; set; }

    /// <summary>
    /// Gets or sets the reason or notes for the moderation action.
    /// </summary>
    [MaxLength(1000)]
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the previous moderation status before the action (for audit trail).
    /// </summary>
    public ProductModerationStatus? PreviousStatus { get; set; }

    /// <summary>
    /// Gets or sets the new moderation status after the action (for audit trail).
    /// </summary>
    public ProductModerationStatus? NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the action was taken.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
