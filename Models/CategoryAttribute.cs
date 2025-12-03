using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents an attribute template definition for a category.
/// Defines structured fields that products in this category should have.
/// </summary>
public class CategoryAttribute
{
    /// <summary>
    /// Gets or sets the unique identifier for the category attribute.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the category ID this attribute belongs to.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the category this attribute belongs to (navigation property).
    /// </summary>
    public Category Category { get; set; } = null!;

    /// <summary>
    /// Gets or sets the attribute name (e.g., "Brand", "Size", "Color").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the attribute display label (used in UI).
    /// If not set, the Name is used.
    /// </summary>
    [MaxLength(100)]
    public string? DisplayLabel { get; set; }

    /// <summary>
    /// Gets or sets the attribute description or help text.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the attribute data type.
    /// </summary>
    public AttributeType AttributeType { get; set; }

    /// <summary>
    /// Gets or sets whether this attribute is required for products in this category.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets whether this attribute is deprecated.
    /// Deprecated attributes are hidden from new product creation but remain visible for existing products.
    /// </summary>
    public bool IsDeprecated { get; set; }

    /// <summary>
    /// Gets or sets whether this attribute can be used for search filtering.
    /// </summary>
    public bool IsFilterable { get; set; }

    /// <summary>
    /// Gets or sets whether this attribute is searchable (included in product search).
    /// </summary>
    public bool IsSearchable { get; set; }

    /// <summary>
    /// Gets or sets the display order for this attribute (lower values appear first).
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Gets or sets the validation pattern (regex) for text-type attributes.
    /// </summary>
    [MaxLength(500)]
    public string? ValidationPattern { get; set; }

    /// <summary>
    /// Gets or sets the validation error message to display when validation fails.
    /// </summary>
    [MaxLength(200)]
    public string? ValidationMessage { get; set; }

    /// <summary>
    /// Gets or sets the minimum value for number-type attributes.
    /// </summary>
    public decimal? MinValue { get; set; }

    /// <summary>
    /// Gets or sets the maximum value for number-type attributes.
    /// </summary>
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// Gets or sets the unit of measurement for number-type attributes (e.g., "cm", "kg").
    /// </summary>
    [MaxLength(20)]
    public string? Unit { get; set; }

    /// <summary>
    /// Gets or sets the predefined options for select-type attributes (navigation property).
    /// </summary>
    public ICollection<CategoryAttributeOption> Options { get; set; } = new List<CategoryAttributeOption>();

    /// <summary>
    /// Gets or sets the date and time when the attribute was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the attribute was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the attribute was deprecated.
    /// Null if not deprecated.
    /// </summary>
    public DateTime? DeprecatedAt { get; set; }
}
