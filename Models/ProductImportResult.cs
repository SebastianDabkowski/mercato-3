using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents the result of importing a single product row.
/// </summary>
public class ProductImportResult
{
    /// <summary>
    /// Gets or sets the unique identifier for this result.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the import job ID this result belongs to.
    /// </summary>
    public int JobId { get; set; }

    /// <summary>
    /// Gets or sets the import job this result belongs to (navigation property).
    /// </summary>
    public ProductImportJob Job { get; set; } = null!;

    /// <summary>
    /// Gets or sets the row number in the source file (1-based, excluding header).
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Gets or sets whether this row was successfully imported.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets whether this was a new product creation (true) or update (false).
    /// Only set if Success is true.
    /// </summary>
    public bool? IsCreate { get; set; }

    /// <summary>
    /// Gets or sets the product ID that was created or updated.
    /// Only set if Success is true.
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// Gets or sets the SKU from the import row.
    /// </summary>
    [MaxLength(100)]
    public string? Sku { get; set; }

    /// <summary>
    /// Gets or sets the title from the import row.
    /// </summary>
    [MaxLength(200)]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the description from the import row.
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the price from the import row.
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Gets or sets the stock from the import row.
    /// </summary>
    public int? Stock { get; set; }

    /// <summary>
    /// Gets or sets the category from the import row.
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the weight from the import row.
    /// </summary>
    public decimal? Weight { get; set; }

    /// <summary>
    /// Gets or sets the length from the import row.
    /// </summary>
    public decimal? Length { get; set; }

    /// <summary>
    /// Gets or sets the width from the import row.
    /// </summary>
    public decimal? Width { get; set; }

    /// <summary>
    /// Gets or sets the height from the import row.
    /// </summary>
    public decimal? Height { get; set; }

    /// <summary>
    /// Gets or sets the shipping methods from the import row.
    /// </summary>
    [MaxLength(500)]
    public string? ShippingMethods { get; set; }

    /// <summary>
    /// Gets or sets the error message if the import failed.
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
}
