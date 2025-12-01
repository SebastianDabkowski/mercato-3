using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Result of a variant operation.
/// </summary>
public class VariantResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Data for creating variant attributes.
/// </summary>
public class VariantAttributeData
{
    public required string Name { get; set; }
    public List<string> Values { get; set; } = new();
}

/// <summary>
/// Data for creating a product variant.
/// </summary>
public class CreateVariantData
{
    public string? Sku { get; set; }
    public int Stock { get; set; }
    public decimal? PriceOverride { get; set; }
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, string> AttributeValues { get; set; } = new();
}

/// <summary>
/// Data for updating a product variant.
/// </summary>
public class UpdateVariantData
{
    public string? Sku { get; set; }
    public int Stock { get; set; }
    public decimal? PriceOverride { get; set; }
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Interface for product variant service.
/// </summary>
public interface IProductVariantService
{
    /// <summary>
    /// Enables variants for a product and creates variant attributes.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="storeId">The store ID for ownership verification.</param>
    /// <param name="attributes">The variant attributes to create.</param>
    Task<VariantResult> EnableVariantsAsync(int productId, int storeId, List<VariantAttributeData> attributes);

    /// <summary>
    /// Disables variants for a product and removes all variant data.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="storeId">The store ID for ownership verification.</param>
    Task<VariantResult> DisableVariantsAsync(int productId, int storeId);

    /// <summary>
    /// Gets all variant attributes for a product with their values.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="storeId">The store ID for ownership verification (null for public access).</param>
    Task<List<ProductVariantAttribute>> GetVariantAttributesAsync(int productId, int? storeId = null);

    /// <summary>
    /// Gets all variants for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="storeId">The store ID for ownership verification (null for public access).</param>
    Task<List<ProductVariant>> GetVariantsAsync(int productId, int? storeId = null);

    /// <summary>
    /// Creates a new variant for a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="storeId">The store ID for ownership verification.</param>
    /// <param name="data">The variant data.</param>
    Task<VariantResult> CreateVariantAsync(int productId, int storeId, CreateVariantData data);

    /// <summary>
    /// Updates an existing variant.
    /// </summary>
    /// <param name="variantId">The variant ID.</param>
    /// <param name="storeId">The store ID for ownership verification.</param>
    /// <param name="data">The updated variant data.</param>
    Task<VariantResult> UpdateVariantAsync(int variantId, int storeId, UpdateVariantData data);

    /// <summary>
    /// Deletes a variant.
    /// </summary>
    /// <param name="variantId">The variant ID.</param>
    /// <param name="storeId">The store ID for ownership verification.</param>
    Task<VariantResult> DeleteVariantAsync(int variantId, int storeId);

    /// <summary>
    /// Gets a specific variant by ID.
    /// </summary>
    /// <param name="variantId">The variant ID.</param>
    /// <param name="storeId">The store ID for ownership verification (null for public access).</param>
    Task<ProductVariant?> GetVariantByIdAsync(int variantId, int? storeId = null);

    /// <summary>
    /// Generates all possible variant combinations for a product based on its attributes.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="storeId">The store ID for ownership verification.</param>
    Task<VariantResult> GenerateVariantCombinationsAsync(int productId, int storeId);
}

/// <summary>
/// Service for managing product variants.
/// </summary>
public class ProductVariantService : IProductVariantService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductVariantService> _logger;

    public ProductVariantService(
        ApplicationDbContext context,
        ILogger<ProductVariantService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<VariantResult> EnableVariantsAsync(int productId, int storeId, List<VariantAttributeData> attributes)
    {
        var result = new VariantResult();

        // Validate product exists and belongs to store
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.StoreId == storeId);

        if (product == null)
        {
            result.Errors.Add("Product not found.");
            return result;
        }

        if (product.HasVariants)
        {
            result.Errors.Add("Product already has variants enabled.");
            return result;
        }

        if (attributes.Count == 0)
        {
            result.Errors.Add("At least one variant attribute is required.");
            return result;
        }

        // Validate attributes
        foreach (var attr in attributes)
        {
            if (string.IsNullOrWhiteSpace(attr.Name))
            {
                result.Errors.Add("Attribute name cannot be empty.");
                return result;
            }

            if (attr.Values.Count == 0)
            {
                result.Errors.Add($"Attribute '{attr.Name}' must have at least one value.");
                return result;
            }
        }

        try
        {
            // Enable variants on product
            product.HasVariants = true;

            // Create variant attributes
            var displayOrder = 0;
            foreach (var attr in attributes)
            {
                var attribute = new ProductVariantAttribute
                {
                    ProductId = productId,
                    Name = attr.Name,
                    DisplayOrder = displayOrder++
                };

                _context.ProductVariantAttributes.Add(attribute);
                await _context.SaveChangesAsync(); // Save to get the ID

                // Create attribute values
                var valueOrder = 0;
                foreach (var value in attr.Values)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        _context.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
                        {
                            VariantAttributeId = attribute.Id,
                            Value = value,
                            DisplayOrder = valueOrder++
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;

            _logger.LogInformation("Enabled variants for product {ProductId}", productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling variants for product {ProductId}", productId);
            result.Errors.Add("An error occurred while enabling variants.");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<VariantResult> DisableVariantsAsync(int productId, int storeId)
    {
        var result = new VariantResult();

        // Validate product exists and belongs to store
        var product = await _context.Products
            .Include(p => p.VariantAttributes)
                .ThenInclude(a => a.Values)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == productId && p.StoreId == storeId);

        if (product == null)
        {
            result.Errors.Add("Product not found.");
            return result;
        }

        if (!product.HasVariants)
        {
            result.Errors.Add("Product does not have variants enabled.");
            return result;
        }

        try
        {
            // Remove all variants and attributes
            _context.ProductVariants.RemoveRange(product.Variants);
            _context.ProductVariantAttributes.RemoveRange(product.VariantAttributes);

            product.HasVariants = false;
            await _context.SaveChangesAsync();
            result.Success = true;

            _logger.LogInformation("Disabled variants for product {ProductId}", productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling variants for product {ProductId}", productId);
            result.Errors.Add("An error occurred while disabling variants.");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<List<ProductVariantAttribute>> GetVariantAttributesAsync(int productId, int? storeId = null)
    {
        var query = _context.ProductVariantAttributes
            .Include(a => a.Values.OrderBy(v => v.DisplayOrder))
            .Where(a => a.ProductId == productId);

        if (storeId.HasValue)
        {
            query = query.Where(a => a.Product.StoreId == storeId.Value);
        }

        return await query.OrderBy(a => a.DisplayOrder).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<ProductVariant>> GetVariantsAsync(int productId, int? storeId = null)
    {
        var query = _context.ProductVariants
            .Include(v => v.Options)
                .ThenInclude(o => o.AttributeValue)
                    .ThenInclude(av => av.VariantAttribute)
            .Include(v => v.Images.OrderBy(i => i.DisplayOrder))
            .Where(v => v.ProductId == productId);

        if (storeId.HasValue)
        {
            query = query.Where(v => v.Product.StoreId == storeId.Value);
        }
        else
        {
            // For public access, only return enabled variants
            query = query.Where(v => v.IsEnabled);
        }

        return await query.OrderBy(v => v.Id).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<VariantResult> CreateVariantAsync(int productId, int storeId, CreateVariantData data)
    {
        var result = new VariantResult();

        // Validate product exists and has variants enabled
        var product = await _context.Products
            .Include(p => p.VariantAttributes)
                .ThenInclude(a => a.Values)
            .FirstOrDefaultAsync(p => p.Id == productId && p.StoreId == storeId);

        if (product == null)
        {
            result.Errors.Add("Product not found.");
            return result;
        }

        if (!product.HasVariants)
        {
            result.Errors.Add("Product does not have variants enabled.");
            return result;
        }

        // Validate stock
        if (data.Stock < 0)
        {
            result.Errors.Add("Stock cannot be negative.");
            return result;
        }

        // Validate price override
        if (data.PriceOverride.HasValue && (data.PriceOverride.Value < 0.01m || data.PriceOverride.Value > 999999.99m))
        {
            result.Errors.Add("Price override must be between 0.01 and 999,999.99.");
            return result;
        }

        // Validate attribute values
        if (data.AttributeValues.Count != product.VariantAttributes.Count)
        {
            result.Errors.Add("Must specify a value for each variant attribute.");
            return result;
        }

        var attributeValueIds = new List<int>();
        foreach (var attr in product.VariantAttributes)
        {
            if (!data.AttributeValues.TryGetValue(attr.Name, out var valueName))
            {
                result.Errors.Add($"Missing value for attribute '{attr.Name}'.");
                return result;
            }

            var value = attr.Values.FirstOrDefault(v => v.Value == valueName);
            if (value == null)
            {
                result.Errors.Add($"Invalid value '{valueName}' for attribute '{attr.Name}'.");
                return result;
            }

            attributeValueIds.Add(value.Id);
        }

        // Check if variant with these attribute values already exists
        var existingVariants = await _context.ProductVariants
            .Include(v => v.Options)
            .Where(v => v.ProductId == productId)
            .ToListAsync();

        foreach (var existingVariant in existingVariants)
        {
            var existingValueIds = existingVariant.Options.Select(o => o.AttributeValueId).OrderBy(id => id).ToList();
            var newValueIds = attributeValueIds.OrderBy(id => id).ToList();

            if (existingValueIds.SequenceEqual(newValueIds))
            {
                result.Errors.Add("A variant with these attribute values already exists.");
                return result;
            }
        }

        try
        {
            var variant = new ProductVariant
            {
                ProductId = productId,
                Sku = data.Sku,
                Stock = data.Stock,
                PriceOverride = data.PriceOverride,
                IsEnabled = data.IsEnabled
            };

            _context.ProductVariants.Add(variant);
            await _context.SaveChangesAsync();

            // Create variant options
            foreach (var valueId in attributeValueIds)
            {
                _context.ProductVariantOptions.Add(new ProductVariantOption
                {
                    ProductVariantId = variant.Id,
                    AttributeValueId = valueId
                });
            }

            await _context.SaveChangesAsync();
            result.Success = true;

            _logger.LogInformation("Created variant {VariantId} for product {ProductId}", variant.Id, productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating variant for product {ProductId}", productId);
            result.Errors.Add("An error occurred while creating the variant.");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<VariantResult> UpdateVariantAsync(int variantId, int storeId, UpdateVariantData data)
    {
        var result = new VariantResult();

        var variant = await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId && v.Product.StoreId == storeId);

        if (variant == null)
        {
            result.Errors.Add("Variant not found.");
            return result;
        }

        // Validate stock
        if (data.Stock < 0)
        {
            result.Errors.Add("Stock cannot be negative.");
            return result;
        }

        // Validate price override
        if (data.PriceOverride.HasValue && (data.PriceOverride.Value < 0.01m || data.PriceOverride.Value > 999999.99m))
        {
            result.Errors.Add("Price override must be between 0.01 and 999,999.99.");
            return result;
        }

        try
        {
            variant.Sku = data.Sku;
            variant.Stock = data.Stock;
            variant.PriceOverride = data.PriceOverride;
            variant.IsEnabled = data.IsEnabled;
            variant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            result.Success = true;

            _logger.LogInformation("Updated variant {VariantId}", variantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating variant {VariantId}", variantId);
            result.Errors.Add("An error occurred while updating the variant.");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<VariantResult> DeleteVariantAsync(int variantId, int storeId)
    {
        var result = new VariantResult();

        var variant = await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId && v.Product.StoreId == storeId);

        if (variant == null)
        {
            result.Errors.Add("Variant not found.");
            return result;
        }

        try
        {
            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync();
            result.Success = true;

            _logger.LogInformation("Deleted variant {VariantId}", variantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting variant {VariantId}", variantId);
            result.Errors.Add("An error occurred while deleting the variant.");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ProductVariant?> GetVariantByIdAsync(int variantId, int? storeId = null)
    {
        var query = _context.ProductVariants
            .Include(v => v.Options)
                .ThenInclude(o => o.AttributeValue)
                    .ThenInclude(av => av.VariantAttribute)
            .Include(v => v.Images.OrderBy(i => i.DisplayOrder))
            .Where(v => v.Id == variantId);

        if (storeId.HasValue)
        {
            query = query.Where(v => v.Product.StoreId == storeId.Value);
        }
        else
        {
            // For public access, only return enabled variants
            query = query.Where(v => v.IsEnabled);
        }

        return await query.FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<VariantResult> GenerateVariantCombinationsAsync(int productId, int storeId)
    {
        var result = new VariantResult();

        // Get product with attributes
        var product = await _context.Products
            .Include(p => p.VariantAttributes)
                .ThenInclude(a => a.Values)
            .FirstOrDefaultAsync(p => p.Id == productId && p.StoreId == storeId);

        if (product == null)
        {
            result.Errors.Add("Product not found.");
            return result;
        }

        if (!product.HasVariants)
        {
            result.Errors.Add("Product does not have variants enabled.");
            return result;
        }

        if (product.VariantAttributes.Count == 0)
        {
            result.Errors.Add("No variant attributes defined.");
            return result;
        }

        try
        {
            // Get existing variants
            var existingVariants = await _context.ProductVariants
                .Include(v => v.Options)
                .Where(v => v.ProductId == productId)
                .ToListAsync();

            // Generate all combinations
            var attributeLists = product.VariantAttributes
                .OrderBy(a => a.DisplayOrder)
                .Select(a => a.Values.OrderBy(v => v.DisplayOrder).ToList())
                .ToList();

            var combinations = GenerateCombinations(attributeLists);

            // Create variants for combinations that don't exist
            foreach (var combination in combinations)
            {
                var valueIds = combination.Select(v => v.Id).OrderBy(id => id).ToList();

                // Check if this combination already exists
                var exists = existingVariants.Any(ev =>
                {
                    var existingValueIds = ev.Options.Select(o => o.AttributeValueId).OrderBy(id => id).ToList();
                    return existingValueIds.SequenceEqual(valueIds);
                });

                if (!exists)
                {
                    var variant = new ProductVariant
                    {
                        ProductId = productId,
                        Stock = 0,
                        IsEnabled = true
                    };

                    _context.ProductVariants.Add(variant);
                }
            }

            // Save all new variants at once
            await _context.SaveChangesAsync();

            // Get the newly created variants (those without options)
            var newVariants = await _context.ProductVariants
                .Where(v => v.ProductId == productId)
                .Include(v => v.Options)
                .ToListAsync();

            // Create options for variants that don't have them yet
            foreach (var combination in combinations)
            {
                var valueIds = combination.Select(v => v.Id).OrderBy(id => id).ToList();
                
                // Check if this combination already has a variant with options
                var variantWithOptions = newVariants.FirstOrDefault(v =>
                {
                    if (v.Options.Count == 0) return false;
                    var existingValueIds = v.Options.Select(o => o.AttributeValueId).OrderBy(id => id).ToList();
                    return existingValueIds.SequenceEqual(valueIds);
                });

                if (variantWithOptions == null)
                {
                    // Find a variant without options
                    var variantWithoutOptions = newVariants.FirstOrDefault(v => v.Options.Count == 0);
                    if (variantWithoutOptions != null)
                    {
                        foreach (var valueId in valueIds)
                        {
                            var option = new ProductVariantOption
                            {
                                ProductVariantId = variantWithoutOptions.Id,
                                AttributeValueId = valueId
                            };
                            _context.ProductVariantOptions.Add(option);
                            variantWithoutOptions.Options.Add(option);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;

            _logger.LogInformation("Generated variant combinations for product {ProductId}", productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating variant combinations for product {ProductId}", productId);
            result.Errors.Add("An error occurred while generating variant combinations.");
        }

        return result;
    }

    private List<List<ProductVariantAttributeValue>> GenerateCombinations(List<List<ProductVariantAttributeValue>> lists)
    {
        if (lists.Count == 0)
            return new List<List<ProductVariantAttributeValue>>();

        if (lists.Count == 1)
            return lists[0].Select(v => new List<ProductVariantAttributeValue> { v }).ToList();

        var result = new List<List<ProductVariantAttributeValue>>();
        var remainingCombinations = GenerateCombinations(lists.Skip(1).ToList());

        foreach (var value in lists[0])
        {
            foreach (var combination in remainingCombinations)
            {
                var newCombination = new List<ProductVariantAttributeValue> { value };
                newCombination.AddRange(combination);
                result.Add(newCombination);
            }
        }

        return result;
    }
}
