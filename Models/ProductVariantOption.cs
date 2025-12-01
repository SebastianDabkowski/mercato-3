namespace MercatoApp.Models;

/// <summary>
/// Represents a single attribute-value pair for a product variant.
/// For example, "Size=Small" or "Color=Red".
/// </summary>
public class ProductVariantOption
{
    /// <summary>
    /// Gets or sets the unique identifier for the variant option.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the product variant ID this option belongs to.
    /// </summary>
    public int ProductVariantId { get; set; }

    /// <summary>
    /// Gets or sets the product variant this option belongs to (navigation property).
    /// </summary>
    public ProductVariant ProductVariant { get; set; } = null!;

    /// <summary>
    /// Gets or sets the attribute value ID for this option.
    /// </summary>
    public int AttributeValueId { get; set; }

    /// <summary>
    /// Gets or sets the attribute value for this option (navigation property).
    /// </summary>
    public ProductVariantAttributeValue AttributeValue { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date and time when the option was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
