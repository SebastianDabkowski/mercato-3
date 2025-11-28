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
/// Data for updating an existing product.
/// </summary>
public class UpdateProductData
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public required string Category { get; set; }
    public ProductStatus Status { get; set; }
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
    /// Updates an existing product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="storeId">The store ID for ownership verification.</param>
    /// <param name="data">The updated product data.</param>
    /// <param name="userId">The user ID performing the update (for audit logging).</param>
    Task<ProductResult> UpdateProductAsync(int productId, int storeId, UpdateProductData data, int userId);

    /// <summary>
    /// Archives a product (soft delete).
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="storeId">The store ID for ownership verification.</param>
    /// <param name="userId">The user ID performing the archive (for audit logging).</param>
    Task<ProductResult> ArchiveProductAsync(int productId, int storeId, int userId);

    /// <summary>
    /// Gets all products for a store.
    /// </summary>
    Task<List<Product>> GetProductsByStoreIdAsync(int storeId);

    /// <summary>
    /// Gets all non-archived products for a store.
    /// </summary>
    Task<List<Product>> GetActiveProductsByStoreIdAsync(int storeId);

    /// <summary>
    /// Gets a product by its ID, optionally filtered by store.
    /// </summary>
    Task<Product?> GetProductByIdAsync(int productId, int? storeId = null);

    /// <summary>
    /// Gets a product for public view (only active, non-archived products).
    /// </summary>
    Task<Product?> GetProductForPublicViewAsync(int productId);
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
    public async Task<List<Product>> GetActiveProductsByStoreIdAsync(int storeId)
    {
        return await _context.Products
            .Where(p => p.StoreId == storeId && p.Status != ProductStatus.Archived)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Product?> GetProductByIdAsync(int productId, int? storeId = null)
    {
        var query = _context.Products.Include(p => p.Store).AsQueryable();

        if (storeId.HasValue)
        {
            query = query.Where(p => p.StoreId == storeId.Value);
        }

        return await query.FirstOrDefaultAsync(p => p.Id == productId);
    }

    /// <inheritdoc />
    public async Task<Product?> GetProductForPublicViewAsync(int productId)
    {
        return await _context.Products
            .Include(p => p.Store)
            .FirstOrDefaultAsync(p => p.Id == productId && p.Status == ProductStatus.Active);
    }

    /// <inheritdoc />
    public async Task<ProductResult> UpdateProductAsync(int productId, int storeId, UpdateProductData data, int userId)
    {
        var result = new ProductResult();

        // Get the product with store verification
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.StoreId == storeId);

        if (product == null)
        {
            result.Errors.Add("Product not found or you do not have permission to edit it.");
            return result;
        }

        // Cannot edit archived products
        if (product.Status == ProductStatus.Archived)
        {
            result.Errors.Add("Cannot edit an archived product.");
            return result;
        }

        // Validate input data
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

        // Validate status transition - cannot set to Archived via update
        if (data.Status == ProductStatus.Archived)
        {
            result.Errors.Add("Use the archive function to archive a product.");
        }

        if (result.Errors.Count > 0)
        {
            return result;
        }

        // Log changes for audit
        var changes = new List<string>();
        if (product.Title != data.Title.Trim())
        {
            changes.Add($"Title: '{product.Title}' -> '{data.Title.Trim()}'");
        }
        if (product.Description != data.Description?.Trim())
        {
            changes.Add("Description changed");
        }
        if (product.Price != data.Price)
        {
            changes.Add($"Price: {product.Price:C} -> {data.Price:C}");
        }
        if (product.Stock != data.Stock)
        {
            changes.Add($"Stock: {product.Stock} -> {data.Stock}");
        }
        if (product.Category != data.Category.Trim())
        {
            changes.Add($"Category: '{product.Category}' -> '{data.Category.Trim()}'");
        }
        if (product.Status != data.Status)
        {
            changes.Add($"Status: {product.Status} -> {data.Status}");
        }

        // Update the product
        product.Title = data.Title.Trim();
        product.Description = data.Description?.Trim();
        product.Price = data.Price;
        product.Stock = data.Stock;
        product.Category = data.Category.Trim();
        product.Status = data.Status;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        if (changes.Count > 0)
        {
            _logger.LogInformation(
                "Product {ProductId} updated by user {UserId}. Changes: {Changes}",
                productId,
                userId,
                string.Join("; ", changes));
        }

        result.Success = true;
        result.Product = product;
        return result;
    }

    /// <inheritdoc />
    public async Task<ProductResult> ArchiveProductAsync(int productId, int storeId, int userId)
    {
        var result = new ProductResult();

        // Get the product with store verification
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.StoreId == storeId);

        if (product == null)
        {
            result.Errors.Add("Product not found or you do not have permission to delete it.");
            return result;
        }

        // Already archived
        if (product.Status == ProductStatus.Archived)
        {
            result.Errors.Add("Product is already archived.");
            return result;
        }

        var previousStatus = product.Status;

        // Archive the product (soft delete)
        product.Status = ProductStatus.Archived;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Product {ProductId} archived by user {UserId}. Previous status: {PreviousStatus}",
            productId,
            userId,
            previousStatus);

        result.Success = true;
        result.Product = product;
        return result;
    }
}
