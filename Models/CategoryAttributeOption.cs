using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a predefined option for a select-type category attribute.
/// </summary>
public class CategoryAttributeOption
{
    /// <summary>
    /// Gets or sets the unique identifier for the attribute option.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the category attribute ID this option belongs to.
    /// </summary>
    public int CategoryAttributeId { get; set; }

    /// <summary>
    /// Gets or sets the category attribute this option belongs to (navigation property).
    /// </summary>
    public CategoryAttribute CategoryAttribute { get; set; } = null!;

    /// <summary>
    /// Gets or sets the option value (e.g., "Red", "Blue", "Large", "Small").
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display label for the option (used in UI).
    /// If not set, the Value is used.
    /// </summary>
    [MaxLength(200)]
    public string? DisplayLabel { get; set; }

    /// <summary>
    /// Gets or sets the display order for this option (lower values appear first).
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether this option is active.
    /// Inactive options are hidden from new product creation but remain valid for existing products.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the option was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
