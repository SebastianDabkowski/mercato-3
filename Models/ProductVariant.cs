using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a specific variant of a product (e.g., "Small Red Cotton Shirt").
/// </summary>
public class ProductVariant
{
    /// <summary>
    /// Gets or sets the unique identifier for the product variant.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the product ID this variant belongs to.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product this variant belongs to (navigation property).
    /// </summary>
    public Product Product { get; set; } = null!;

    /// <summary>
    /// Gets or sets the variant-specific SKU (Stock Keeping Unit).
    /// If not set, the parent product's SKU is used.
    /// </summary>
    [MaxLength(100)]
    public string? Sku { get; set; }

    /// <summary>
    /// Gets or sets the stock quantity for this specific variant.
    /// </summary>
    [Required]
    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    /// <summary>
    /// Gets or sets the price override for this variant.
    /// If null, the parent product's price is used.
    /// </summary>
    [Range(0.01, 999999.99)]
    public decimal? PriceOverride { get; set; }

    /// <summary>
    /// Gets or sets whether this variant is enabled.
    /// Disabled variants are not shown to buyers.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the variant was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the variant was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the variant options that define this variant (navigation property).
    /// For example, a "Small Red" variant would have two options: Size=Small and Color=Red.
    /// </summary>
    public ICollection<ProductVariantOption> Options { get; set; } = new List<ProductVariantOption>();

    /// <summary>
    /// Gets or sets the images specific to this variant (navigation property).
    /// </summary>
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
