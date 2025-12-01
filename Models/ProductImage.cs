using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents an image associated with a product.
/// </summary>
public class ProductImage
{
    /// <summary>
    /// Gets or sets the unique identifier for the product image.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the product ID this image belongs to.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product this image belongs to (navigation property).
    /// </summary>
    public Product Product { get; set; } = null!;

    /// <summary>
    /// Gets or sets the variant ID this image is specific to.
    /// If null, the image is for the main product.
    /// </summary>
    public int? VariantId { get; set; }

    /// <summary>
    /// Gets or sets the variant this image is specific to (navigation property).
    /// </summary>
    public ProductVariant? Variant { get; set; }

    /// <summary>
    /// Gets or sets the original file name of the uploaded image.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stored file name (with unique identifier).
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL path to the original image.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL path to the thumbnail version.
    /// </summary>
    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL path to the medium-sized version.
    /// </summary>
    [MaxLength(500)]
    public string? MediumUrl { get; set; }

    /// <summary>
    /// Gets or sets the content type (MIME type) of the image.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the original image width in pixels.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the original image height in pixels.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets whether this is the main/primary image for the product.
    /// </summary>
    public bool IsMain { get; set; }

    /// <summary>
    /// Gets or sets the display order for sorting images.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the image was uploaded.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
