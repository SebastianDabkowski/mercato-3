using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a possible value for a variant attribute (e.g., "Small", "Red").
/// </summary>
public class ProductVariantAttributeValue
{
    /// <summary>
    /// Gets or sets the unique identifier for the attribute value.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the variant attribute ID this value belongs to.
    /// </summary>
    public int VariantAttributeId { get; set; }

    /// <summary>
    /// Gets or sets the variant attribute this value belongs to (navigation property).
    /// </summary>
    public ProductVariantAttribute VariantAttribute { get; set; } = null!;

    /// <summary>
    /// Gets or sets the value (e.g., "Small", "Red", "Cotton").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display order for this value.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the value was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the variant options that use this attribute value (navigation property).
    /// </summary>
    public ICollection<ProductVariantOption> VariantOptions { get; set; } = new List<ProductVariantOption>();
}
