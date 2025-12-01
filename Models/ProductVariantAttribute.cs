using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a variant attribute type for a product (e.g., "Size", "Color").
/// </summary>
public class ProductVariantAttribute
{
    /// <summary>
    /// Gets or sets the unique identifier for the variant attribute.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the product ID this attribute belongs to.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product this attribute belongs to (navigation property).
    /// </summary>
    public Product Product { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the attribute (e.g., "Size", "Color", "Material").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display order for this attribute.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the attribute was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the values for this attribute (navigation property).
    /// </summary>
    public ICollection<ProductVariantAttributeValue> Values { get; set; } = new List<ProductVariantAttributeValue>();
}
