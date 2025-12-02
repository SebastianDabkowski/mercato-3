using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing shipping methods.
/// </summary>
public class ShippingMethodService : IShippingMethodService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ShippingMethodService> _logger;

    public ShippingMethodService(
        ApplicationDbContext context,
        ILogger<ShippingMethodService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ShippingMethod>> GetActiveShippingMethodsAsync(int storeId)
    {
        return await _context.ShippingMethods
            .Where(sm => sm.StoreId == storeId && sm.IsActive)
            .OrderBy(sm => sm.DisplayOrder)
            .ThenBy(sm => sm.BaseCost)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<ShippingMethod>> GetActiveShippingMethodsByCountryAsync(int storeId, string countryCode)
    {
        var methods = await GetActiveShippingMethodsAsync(storeId);
        
        // Filter by country if specified
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return methods;
        }

        var filteredMethods = methods.Where(method =>
        {
            // If no countries specified, available everywhere
            if (string.IsNullOrWhiteSpace(method.AllowedCountries))
            {
                return true;
            }

            // Check if country is in the allowed list
            var allowedCountries = method.AllowedCountries
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(c => c.ToUpperInvariant());

            return allowedCountries.Contains(countryCode.ToUpperInvariant());
        }).ToList();

        return filteredMethods;
    }

    /// <inheritdoc />
    public async Task<List<ShippingMethod>> GetOrCreateDefaultShippingMethodsAsync(int storeId)
    {
        var existingMethods = await GetActiveShippingMethodsAsync(storeId);
        
        if (existingMethods.Any())
        {
            return existingMethods;
        }

        // Create default shipping methods for the store
        var defaultMethods = new List<ShippingMethod>
        {
            new ShippingMethod
            {
                StoreId = storeId,
                Name = "Standard Shipping",
                Description = "Regular delivery service",
                EstimatedDelivery = "5-7 business days",
                BaseCost = 5.00m,
                AdditionalItemCost = 2.00m,
                FreeShippingThreshold = 50.00m,
                IsActive = true,
                DisplayOrder = 1
            },
            new ShippingMethod
            {
                StoreId = storeId,
                Name = "Express Shipping",
                Description = "Fast delivery service",
                EstimatedDelivery = "2-3 business days",
                BaseCost = 15.00m,
                AdditionalItemCost = 3.00m,
                FreeShippingThreshold = null,
                IsActive = true,
                DisplayOrder = 2
            }
        };

        _context.ShippingMethods.AddRange(defaultMethods);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created default shipping methods for store {StoreId}", storeId);

        return defaultMethods;
    }

    /// <inheritdoc />
    public async Task<ShippingMethod?> GetShippingMethodByIdAsync(int id)
    {
        return await _context.ShippingMethods
            .FirstOrDefaultAsync(sm => sm.Id == id);
    }

    /// <inheritdoc />
    public async Task<decimal> CalculateShippingCostAsync(int shippingMethodId, List<CartItem> items)
    {
        var shippingMethod = await GetShippingMethodByIdAsync(shippingMethodId);
        
        if (shippingMethod == null)
        {
            throw new InvalidOperationException("Shipping method not found.");
        }

        var itemsSubtotal = items.Sum(i => i.PriceAtAdd * i.Quantity);
        var itemCount = items.Sum(i => i.Quantity);

        // Check if free shipping threshold is met
        if (shippingMethod.FreeShippingThreshold.HasValue &&
            itemsSubtotal >= shippingMethod.FreeShippingThreshold.Value)
        {
            return 0;
        }

        // Calculate shipping cost: base cost + (additional item cost × (item count - 1))
        var shippingCost = shippingMethod.BaseCost;

        if (itemCount > 1)
        {
            shippingCost += shippingMethod.AdditionalItemCost * (itemCount - 1);
        }

        return shippingCost;
    }

    /// <inheritdoc />
    public async Task<List<ShippingMethod>> GetAllShippingMethodsAsync(int storeId)
    {
        return await _context.ShippingMethods
            .Where(sm => sm.StoreId == storeId)
            .OrderBy(sm => sm.DisplayOrder)
            .ThenBy(sm => sm.BaseCost)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ShippingMethod> CreateShippingMethodAsync(ShippingMethod shippingMethod)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(shippingMethod.Name))
        {
            throw new ArgumentException("Shipping method name is required.", nameof(shippingMethod));
        }

        if (shippingMethod.StoreId <= 0)
        {
            throw new ArgumentException("Valid store ID is required.", nameof(shippingMethod));
        }

        // Validate business rules
        if (shippingMethod.BaseCost < 0)
        {
            throw new ArgumentException("Base cost cannot be negative.", nameof(shippingMethod));
        }

        if (shippingMethod.AdditionalItemCost < 0)
        {
            throw new ArgumentException("Additional item cost cannot be negative.", nameof(shippingMethod));
        }

        if (shippingMethod.FreeShippingThreshold.HasValue && shippingMethod.FreeShippingThreshold.Value < 0)
        {
            throw new ArgumentException("Free shipping threshold cannot be negative.", nameof(shippingMethod));
        }

        shippingMethod.CreatedAt = DateTime.UtcNow;
        shippingMethod.UpdatedAt = DateTime.UtcNow;

        _context.ShippingMethods.Add(shippingMethod);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created shipping method {MethodId} for store {StoreId}", 
            shippingMethod.Id, shippingMethod.StoreId);

        return shippingMethod;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateShippingMethodAsync(ShippingMethod shippingMethod, int storeId)
    {
        var existingMethod = await GetShippingMethodByIdAsync(shippingMethod.Id);
        if (existingMethod == null)
        {
            return false;
        }

        // Authorization check: verify the shipping method belongs to the store
        if (existingMethod.StoreId != storeId)
        {
            _logger.LogWarning("Unauthorized attempt to update shipping method {MethodId} by store {StoreId}", 
                shippingMethod.Id, storeId);
            return false;
        }

        existingMethod.Name = shippingMethod.Name;
        existingMethod.Description = shippingMethod.Description;
        existingMethod.EstimatedDelivery = shippingMethod.EstimatedDelivery;
        existingMethod.BaseCost = shippingMethod.BaseCost;
        existingMethod.AdditionalItemCost = shippingMethod.AdditionalItemCost;
        existingMethod.FreeShippingThreshold = shippingMethod.FreeShippingThreshold;
        existingMethod.IsActive = shippingMethod.IsActive;
        existingMethod.DisplayOrder = shippingMethod.DisplayOrder;
        existingMethod.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated shipping method {MethodId}", shippingMethod.Id);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteShippingMethodAsync(int id, int storeId)
    {
        var shippingMethod = await GetShippingMethodByIdAsync(id);
        if (shippingMethod == null)
        {
            return false;
        }

        // Authorization check: verify the shipping method belongs to the store
        if (shippingMethod.StoreId != storeId)
        {
            _logger.LogWarning("Unauthorized attempt to delete shipping method {MethodId} by store {StoreId}", 
                id, storeId);
            return false;
        }

        // Soft delete - just mark as inactive
        shippingMethod.IsActive = false;
        shippingMethod.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted (soft delete) shipping method {MethodId}", id);

        return true;
    }
}
