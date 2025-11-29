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
    public decimal? Weight { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public string? ShippingMethods { get; set; }
    public string? ImageUrls { get; set; }
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
    public decimal? Weight { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public string? ShippingMethods { get; set; }
    public string? ImageUrls { get; set; }
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

    /// <summary>
    /// Maximum allowed price for a product.
    /// </summary>
    public const decimal MaxPrice = 999999.99m;

    /// <summary>
    /// Maximum length for product title.
    /// </summary>
    public const int MaxTitleLength = 200;

    /// <summary>
    /// Maximum length for product description.
    /// </summary>
    public const int MaxDescriptionLength = 2000;

    /// <summary>
    /// Maximum length for product category.
    /// </summary>
    public const int MaxCategoryLength = 100;

    /// <summary>
    /// Maximum weight for a product in kilograms.
    /// </summary>
    public const decimal MaxWeight = 1000m;

    /// <summary>
    /// Maximum dimension (length, width, height) for a product in centimeters.
    /// </summary>
    public const decimal MaxDimension = 500m;

    /// <summary>
    /// Maximum length for shipping methods string.
    /// </summary>
    public const int MaxShippingMethodsLength = 500;

    /// <summary>
    /// Maximum length for image URLs string.
    /// </summary>
    public const int MaxImageUrlsLength = 2000;

    public ProductService(
        ApplicationDbContext context,
        ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Validates common product fields and adds errors to the result.
    /// </summary>
    private static void ValidateProductFields(
        string? title,
        string? description,
        decimal price,
        int stock,
        string? category,
        ProductResult result)
    {
        // Validate title
        if (string.IsNullOrWhiteSpace(title))
        {
            result.Errors.Add("Product title is required.");
        }
        else if (title.Length > MaxTitleLength)
        {
            result.Errors.Add($"Product title must be {MaxTitleLength} characters or less.");
        }

        // Validate description
        if (description?.Length > MaxDescriptionLength)
        {
            result.Errors.Add($"Description must be {MaxDescriptionLength} characters or less.");
        }

        // Validate price
        if (price <= 0)
        {
            result.Errors.Add("Price must be greater than zero.");
        }
        else if (price > MaxPrice)
        {
            result.Errors.Add($"Price must be less than {MaxPrice + 0.01m:N0}.");
        }

        // Validate stock
        if (stock < 0)
        {
            result.Errors.Add("Stock cannot be negative.");
        }

        // Validate category
        if (string.IsNullOrWhiteSpace(category))
        {
            result.Errors.Add("Category is required.");
        }
        else if (category.Length > MaxCategoryLength)
        {
            result.Errors.Add($"Category must be {MaxCategoryLength} characters or less.");
        }
    }

    /// <summary>
    /// Validates shipping parameters and adds errors to the result.
    /// </summary>
    private static void ValidateShippingParameters(
        decimal? weight,
        decimal? length,
        decimal? width,
        decimal? height,
        string? shippingMethods,
        string? imageUrls,
        ProductResult result)
    {
        // Validate weight
        if (weight.HasValue)
        {
            if (weight.Value < 0)
            {
                result.Errors.Add("Weight cannot be negative.");
            }
            else if (weight.Value > MaxWeight)
            {
                result.Errors.Add($"Weight must be {MaxWeight} kg or less.");
            }
        }

        // Validate length
        if (length.HasValue)
        {
            if (length.Value < 0)
            {
                result.Errors.Add("Length cannot be negative.");
            }
            else if (length.Value > MaxDimension)
            {
                result.Errors.Add($"Length must be {MaxDimension} cm or less.");
            }
        }

        // Validate width
        if (width.HasValue)
        {
            if (width.Value < 0)
            {
                result.Errors.Add("Width cannot be negative.");
            }
            else if (width.Value > MaxDimension)
            {
                result.Errors.Add($"Width must be {MaxDimension} cm or less.");
            }
        }

        // Validate height
        if (height.HasValue)
        {
            if (height.Value < 0)
            {
                result.Errors.Add("Height cannot be negative.");
            }
            else if (height.Value > MaxDimension)
            {
                result.Errors.Add($"Height must be {MaxDimension} cm or less.");
            }
        }

        // Validate shipping methods
        if (shippingMethods?.Length > MaxShippingMethodsLength)
        {
            result.Errors.Add($"Shipping methods must be {MaxShippingMethodsLength} characters or less.");
        }

        // Validate image URLs
        if (imageUrls?.Length > MaxImageUrlsLength)
        {
            result.Errors.Add($"Image URLs must be {MaxImageUrlsLength} characters or less.");
        }
    }

    /// <inheritdoc />
    public async Task<ProductResult> CreateProductAsync(int storeId, CreateProductData data)
    {
        var result = new ProductResult();

        // Validate fields
        ValidateProductFields(data.Title, data.Description, data.Price, data.Stock, data.Category, result);
        ValidateShippingParameters(data.Weight, data.Length, data.Width, data.Height, data.ShippingMethods, data.ImageUrls, result);

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
            UpdatedAt = DateTime.UtcNow,
            Weight = data.Weight,
            Length = data.Length,
            Width = data.Width,
            Height = data.Height,
            ShippingMethods = data.ShippingMethods?.Trim(),
            ImageUrls = data.ImageUrls?.Trim()
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

        // Trim input values once
        var trimmedTitle = data.Title?.Trim() ?? string.Empty;
        var trimmedDescription = data.Description?.Trim();
        var trimmedCategory = data.Category?.Trim() ?? string.Empty;
        var trimmedShippingMethods = data.ShippingMethods?.Trim();
        var trimmedImageUrls = data.ImageUrls?.Trim();

        // Validate input data using helper
        ValidateProductFields(trimmedTitle, trimmedDescription, data.Price, data.Stock, trimmedCategory, result);
        ValidateShippingParameters(data.Weight, data.Length, data.Width, data.Height, trimmedShippingMethods, trimmedImageUrls, result);

        // Validate status transition - cannot set to Archived via update
        if (data.Status == ProductStatus.Archived)
        {
            result.Errors.Add("Use the archive function to archive a product.");
        }

        // Validate workflow transition is allowed
        if (product.Status != data.Status)
        {
            if (!ProductWorkflowService.IsTransitionAllowedStatic(product.Status, data.Status, isAdmin: false))
            {
                result.Errors.Add($"Cannot transition from '{product.Status}' to '{data.Status}'. This transition is not allowed.");
            }
        }

        if (result.Errors.Count > 0)
        {
            return result;
        }

        // If transitioning to Active, validate data quality requirements
        if (data.Status == ProductStatus.Active && product.Status != ProductStatus.Active)
        {
            var activationErrors = ProductWorkflowService.ValidateForActivationStatic(
                trimmedTitle,
                trimmedDescription,
                trimmedCategory,
                data.Price,
                data.Stock,
                trimmedImageUrls);
            if (activationErrors.Count > 0)
            {
                result.Errors.AddRange(activationErrors);
                return result;
            }
        }

        // Log changes for audit
        var changes = new List<string>();
        if (product.Title != trimmedTitle)
        {
            changes.Add($"Title: '{product.Title}' -> '{trimmedTitle}'");
        }
        if (product.Description != trimmedDescription)
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
        if (product.Category != trimmedCategory)
        {
            changes.Add($"Category: '{product.Category}' -> '{trimmedCategory}'");
        }
        if (product.Status != data.Status)
        {
            changes.Add($"Status: {product.Status} -> {data.Status}");
        }
        if (product.Weight != data.Weight)
        {
            changes.Add($"Weight: {product.Weight} -> {data.Weight}");
        }
        if (product.Length != data.Length)
        {
            changes.Add($"Length: {product.Length} -> {data.Length}");
        }
        if (product.Width != data.Width)
        {
            changes.Add($"Width: {product.Width} -> {data.Width}");
        }
        if (product.Height != data.Height)
        {
            changes.Add($"Height: {product.Height} -> {data.Height}");
        }
        if (product.ShippingMethods != trimmedShippingMethods)
        {
            changes.Add("Shipping methods changed");
        }
        if (product.ImageUrls != trimmedImageUrls)
        {
            changes.Add("Image URLs changed");
        }

        // Update the product
        product.Title = trimmedTitle;
        product.Description = trimmedDescription;
        product.Price = data.Price;
        product.Stock = data.Stock;
        product.Category = trimmedCategory;
        product.Status = data.Status;
        product.Weight = data.Weight;
        product.Length = data.Length;
        product.Width = data.Width;
        product.Height = data.Height;
        product.ShippingMethods = trimmedShippingMethods;
        product.ImageUrls = trimmedImageUrls;
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
