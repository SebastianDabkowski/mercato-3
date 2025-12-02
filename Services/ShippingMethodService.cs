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
}
