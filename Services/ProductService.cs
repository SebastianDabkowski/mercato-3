using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Result of a product operation.
/// </summary>
public class ProductResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public Product? Product { get; set; }
}

/// <summary>
/// Data for creating a new product.
/// </summary>
public class CreateProductData
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public required string Category { get; set; }
}

/// <summary>
/// Interface for product service.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Creates a new product for the specified store.
    /// </summary>
    Task<ProductResult> CreateProductAsync(int storeId, CreateProductData data);

    /// <summary>
    /// Gets all products for a store.
    /// </summary>
    Task<List<Product>> GetProductsByStoreIdAsync(int storeId);

    /// <summary>
    /// Gets a product by its ID, optionally filtered by store.
    /// </summary>
    Task<Product?> GetProductByIdAsync(int productId, int? storeId = null);
}

/// <summary>
/// Service for managing products.
/// </summary>
public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        ApplicationDbContext context,
        ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ProductResult> CreateProductAsync(int storeId, CreateProductData data)
    {
        var result = new ProductResult();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(data.Title))
        {
            result.Errors.Add("Product title is required.");
        }
        else if (data.Title.Length > 200)
        {
            result.Errors.Add("Product title must be 200 characters or less.");
        }

        if (data.Description?.Length > 2000)
        {
            result.Errors.Add("Description must be 2000 characters or less.");
        }

        if (data.Price <= 0)
        {
            result.Errors.Add("Price must be greater than zero.");
        }
        else if (data.Price > 999999.99m)
        {
            result.Errors.Add("Price must be less than 1,000,000.");
        }

        if (data.Stock < 0)
        {
            result.Errors.Add("Stock cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(data.Category))
        {
            result.Errors.Add("Category is required.");
        }
        else if (data.Category.Length > 100)
        {
            result.Errors.Add("Category must be 100 characters or less.");
        }

        if (result.Errors.Count > 0)
        {
            return result;
        }

        // Verify store exists
        var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == storeId);
        if (store == null)
        {
            result.Errors.Add("Store not found.");
            return result;
        }

        // Create the product
        var product = new Product
        {
            StoreId = storeId,
            Title = data.Title.Trim(),
            Description = data.Description?.Trim(),
            Price = data.Price,
            Stock = data.Stock,
            Category = data.Category.Trim(),
            Status = ProductStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created product {ProductId} for store {StoreId}", product.Id, storeId);

        result.Success = true;
        result.Product = product;
        return result;
    }

    /// <inheritdoc />
    public async Task<List<Product>> GetProductsByStoreIdAsync(int storeId)
    {
        return await _context.Products
            .Where(p => p.StoreId == storeId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Product?> GetProductByIdAsync(int productId, int? storeId = null)
    {
        var query = _context.Products.AsQueryable();

        if (storeId.HasValue)
        {
            query = query.Where(p => p.StoreId == storeId.Value);
        }

        return await query.FirstOrDefaultAsync(p => p.Id == productId);
    }
}
