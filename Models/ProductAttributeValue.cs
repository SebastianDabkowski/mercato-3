using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a product's value for a category attribute.
/// Stores the actual attribute data for a specific product.
/// </summary>
public class ProductAttributeValue
{
    /// <summary>
    /// Gets or sets the unique identifier for the product attribute value.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the product ID this attribute value belongs to.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product this attribute value belongs to (navigation property).
    /// </summary>
    public Product Product { get; set; } = null!;

    /// <summary>
    /// Gets or sets the category attribute ID this value is for.
    /// </summary>
    public int CategoryAttributeId { get; set; }

    /// <summary>
    /// Gets or sets the category attribute this value is for (navigation property).
    /// </summary>
    public CategoryAttribute CategoryAttribute { get; set; } = null!;

    /// <summary>
    /// Gets or sets the text value for text-type attributes.
    /// </summary>
    [MaxLength(2000)]
    public string? TextValue { get; set; }

    /// <summary>
    /// Gets or sets the numeric value for number-type attributes.
    /// </summary>
    public decimal? NumericValue { get; set; }

    /// <summary>
    /// Gets or sets the boolean value for boolean-type attributes.
    /// </summary>
    public bool? BooleanValue { get; set; }

    /// <summary>
    /// Gets or sets the date value for date-type attributes.
    /// </summary>
    public DateTime? DateValue { get; set; }

    /// <summary>
    /// Gets or sets the selected option ID for single-select attributes.
    /// </summary>
    public int? SelectedOptionId { get; set; }

    /// <summary>
    /// Gets or sets the selected option for single-select attributes (navigation property).
    /// </summary>
    public CategoryAttributeOption? SelectedOption { get; set; }

    /// <summary>
    /// Gets or sets the comma-separated list of selected option IDs for multi-select attributes.
    /// Stored as comma-separated values for simplicity.
    /// </summary>
    [MaxLength(500)]
    public string? SelectedOptionIds { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the attribute value was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the attribute value was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
