using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a product in a seller's catalog.
/// </summary>
public class Product
{
    /// <summary>
    /// Gets or sets the unique identifier for the product.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the store ID that owns this product.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store that owns this product (navigation property).
    /// </summary>
    public Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the product title.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product description.
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the product price.
    /// </summary>
    [Required]
    [Range(0.01, 999999.99)]
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the stock quantity.
    /// </summary>
    [Required]
    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    /// <summary>
    /// Gets or sets the merchant SKU (Stock Keeping Unit).
    /// Used for inventory management and import/export operations.
    /// Must be unique within the store.
    /// </summary>
    [MaxLength(100)]
    public string? Sku { get; set; }

    /// <summary>
    /// Gets or sets the product category name.
    /// This is the display name stored for backward compatibility.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category ID for the structured category.
    /// Null for products created before category management was implemented.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the structured category (navigation property).
    /// </summary>
    public Category? CategoryEntity { get; set; }

    /// <summary>
    /// Gets or sets the product status.
    /// New products default to Draft status.
    /// </summary>
    public ProductStatus Status { get; set; } = ProductStatus.Draft;

    /// <summary>
    /// Gets or sets the product moderation status.
    /// New products default to Pending moderation.
    /// </summary>
    public ProductModerationStatus ModerationStatus { get; set; } = ProductModerationStatus.Pending;

    /// <summary>
    /// Gets or sets the product condition.
    /// Defaults to New for new products.
    /// </summary>
    public ProductCondition Condition { get; set; } = ProductCondition.New;

    /// <summary>
    /// Gets or sets the date and time when the product was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the product was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the product weight in kilograms.
    /// Used for shipping cost calculation.
    /// </summary>
    [Range(0, 1000)]
    public decimal? Weight { get; set; }

    /// <summary>
    /// Gets or sets the product length in centimeters.
    /// Used for shipping cost calculation.
    /// </summary>
    [Range(0, 500)]
    public decimal? Length { get; set; }

    /// <summary>
    /// Gets or sets the product width in centimeters.
    /// Used for shipping cost calculation.
    /// </summary>
    [Range(0, 500)]
    public decimal? Width { get; set; }

    /// <summary>
    /// Gets or sets the product height in centimeters.
    /// Used for shipping cost calculation.
    /// </summary>
    [Range(0, 500)]
    public decimal? Height { get; set; }

    /// <summary>
    /// Gets or sets the available shipping methods (comma-separated).
    /// e.g., "Standard,Express,Overnight"
    /// </summary>
    [MaxLength(500)]
    public string? ShippingMethods { get; set; }

    /// <summary>
    /// Gets or sets the comma-separated list of image URLs for this product.
    /// This is maintained for backward compatibility.
    /// </summary>
    [MaxLength(2000)]
    public string? ImageUrls { get; set; }

    /// <summary>
    /// Gets or sets the images for this product (navigation property).
    /// </summary>
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    /// <summary>
    /// Gets or sets whether this product has variants enabled.
    /// When false, this is a simple product with a single SKU.
    /// When true, stock and pricing are managed at the variant level.
    /// </summary>
    public bool HasVariants { get; set; }

    /// <summary>
    /// Gets or sets the variant attributes for this product (navigation property).
    /// Only used when HasVariants is true.
    /// </summary>
    public ICollection<ProductVariantAttribute> VariantAttributes { get; set; } = new List<ProductVariantAttribute>();

    /// <summary>
    /// Gets or sets the variants for this product (navigation property).
    /// Only used when HasVariants is true.
    /// </summary>
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

    /// <summary>
    /// Gets or sets the category attribute values for this product (navigation property).
    /// Stores structured attribute data based on the product's category template.
    /// </summary>
    public ICollection<ProductAttributeValue> AttributeValues { get; set; } = new List<ProductAttributeValue>();
}
