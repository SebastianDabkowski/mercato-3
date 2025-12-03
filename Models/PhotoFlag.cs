using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a flag or report on a product photo by users or automated systems.
/// </summary>
public class PhotoFlag
{
    /// <summary>
    /// Gets or sets the unique identifier for the flag.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the product image ID that was flagged.
    /// </summary>
    public int ProductImageId { get; set; }

    /// <summary>
    /// Gets or sets the product image that was flagged (navigation property).
    /// </summary>
    public ProductImage ProductImage { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user ID who flagged the photo (null for automated flags).
    /// </summary>
    public int? FlaggedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user who flagged the photo (navigation property).
    /// </summary>
    public User? FlaggedByUser { get; set; }

    /// <summary>
    /// Gets or sets the reason for the flag.
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this flag was created by an automated system.
    /// </summary>
    public bool IsAutomated { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the flag was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether this flag has been resolved/reviewed.
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the flag was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID who resolved the flag.
    /// </summary>
    public int? ResolvedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the admin who resolved the flag (navigation property).
    /// </summary>
    public User? ResolvedByUser { get; set; }
}
