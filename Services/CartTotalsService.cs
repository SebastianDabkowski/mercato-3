using MercatoApp.Models;
using MercatoApp.Data;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Interface for cart totals calculation operations.
/// </summary>
public interface ICartTotalsService
{
    /// <summary>
    /// Calculates the cart totals including items subtotal, shipping costs, and total amount.
    /// </summary>
    /// <param name="userId">The user ID for authenticated users, null for anonymous users.</param>
    /// <param name="sessionId">The session ID for anonymous users, null for authenticated users.</param>
    /// <param name="includeCommission">Whether to include internal commission calculations (for admin/internal use only).</param>
    /// <returns>The calculated cart totals.</returns>
    Task<CartTotals> CalculateCartTotalsAsync(int? userId, string? sessionId, bool includeCommission = false);

    /// <summary>
    /// Calculates shipping cost for a specific seller's items.
    /// </summary>
    /// <param name="store">The seller's store.</param>
    /// <param name="items">The cart items from this seller.</param>
    /// <returns>The shipping breakdown for this seller.</returns>
    Task<SellerShippingBreakdown> CalculateSellerShippingAsync(Store store, List<CartItem> items);

    /// <summary>
    /// Gets the default shipping rule for a store.
    /// Creates a default rule if none exists.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>The default shipping rule.</returns>
    Task<ShippingRule> GetOrCreateDefaultShippingRuleAsync(int storeId);
}

/// <summary>
/// Service for calculating cart totals including shipping and commissions.
/// </summary>
public class CartTotalsService : ICartTotalsService
{
    private readonly ApplicationDbContext _context;
    private readonly ICartService _cartService;
    private readonly ILogger<CartTotalsService> _logger;

    public CartTotalsService(
        ApplicationDbContext context,
        ICartService cartService,
        ILogger<CartTotalsService> logger)
    {
        _context = context;
        _cartService = cartService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CartTotals> CalculateCartTotalsAsync(int? userId, string? sessionId, bool includeCommission = false)
    {
        var itemsBySeller = await _cartService.GetCartItemsBySellerAsync(userId, sessionId);

        var cartTotals = new CartTotals
        {
            ShippingBreakdown = new List<SellerShippingBreakdown>()
        };

        // Calculate totals for each seller
        foreach (var sellerGroup in itemsBySeller)
        {
            var store = sellerGroup.Key;
            var items = sellerGroup.Value;

            // Calculate shipping for this seller
            var sellerShipping = await CalculateSellerShippingAsync(store, items);
            cartTotals.ShippingBreakdown.Add(sellerShipping);

            // Add to running totals
            cartTotals.ItemsSubtotal += sellerShipping.ItemsSubtotal;
            cartTotals.TotalShipping += sellerShipping.ShippingCost;
        }

        // Calculate total amount payable by buyer
        cartTotals.TotalAmount = cartTotals.ItemsSubtotal + cartTotals.TotalShipping;

        // Calculate internal commission if requested (not visible to buyers)
        if (includeCommission && cartTotals.ItemsSubtotal > 0)
        {
            cartTotals.InternalCommission = await CalculateCommissionAsync(cartTotals.ItemsSubtotal);
        }

        _logger.LogDebug("Calculated cart totals: Items={ItemsSubtotal}, Shipping={TotalShipping}, Total={TotalAmount}",
            cartTotals.ItemsSubtotal, cartTotals.TotalShipping, cartTotals.TotalAmount);

        return cartTotals;
    }

    /// <inheritdoc />
    public async Task<SellerShippingBreakdown> CalculateSellerShippingAsync(Store store, List<CartItem> items)
    {
        var breakdown = new SellerShippingBreakdown
        {
            Store = store,
            ItemCount = items.Sum(i => i.Quantity),
            ItemsSubtotal = items.Sum(i => i.PriceAtAdd * i.Quantity)
        };

        // Get the active shipping rule for this store
        var shippingRule = await _context.ShippingRules
            .FirstOrDefaultAsync(r => r.StoreId == store.Id && r.IsActive);

        // If no shipping rule exists, create a default one
        if (shippingRule == null)
        {
            shippingRule = await GetOrCreateDefaultShippingRuleAsync(store.Id);
        }

        breakdown.AppliedShippingRule = shippingRule;

        // Check if free shipping threshold is met
        if (shippingRule.FreeShippingThreshold.HasValue &&
            breakdown.ItemsSubtotal >= shippingRule.FreeShippingThreshold.Value)
        {
            breakdown.ShippingCost = 0;
            breakdown.IsFreeShipping = true;
            _logger.LogDebug("Free shipping applied for store {StoreId} (subtotal {Subtotal} >= threshold {Threshold})",
                store.Id, breakdown.ItemsSubtotal, shippingRule.FreeShippingThreshold.Value);
        }
        else
        {
            // Calculate shipping cost: base cost + (additional item cost × (item count - 1))
            var itemCount = breakdown.ItemCount;
            breakdown.ShippingCost = shippingRule.BaseCost;

            if (itemCount > 1)
            {
                breakdown.ShippingCost += shippingRule.AdditionalItemCost * (itemCount - 1);
            }

            breakdown.IsFreeShipping = false;
            _logger.LogDebug("Shipping calculated for store {StoreId}: Base={BaseCost}, Items={ItemCount}, Total={ShippingCost}",
                store.Id, shippingRule.BaseCost, itemCount, breakdown.ShippingCost);
        }

        return breakdown;
    }

    /// <inheritdoc />
    public async Task<ShippingRule> GetOrCreateDefaultShippingRuleAsync(int storeId)
    {
        var existingRule = await _context.ShippingRules
            .FirstOrDefaultAsync(r => r.StoreId == storeId);

        if (existingRule != null)
        {
            return existingRule;
        }

        // Create a default shipping rule for the store
        var defaultRule = new ShippingRule
        {
            StoreId = storeId,
            Name = "Standard Shipping",
            BaseCost = 5.00m,
            AdditionalItemCost = 2.00m,
            FreeShippingThreshold = 50.00m,
            IsActive = true
        };

        _context.ShippingRules.Add(defaultRule);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created default shipping rule for store {StoreId}", storeId);

        return defaultRule;
    }

    /// <summary>
    /// Calculates internal commission breakdown (not visible to buyers).
    /// </summary>
    /// <param name="itemsSubtotal">The items subtotal (before shipping).</param>
    /// <returns>The commission breakdown.</returns>
    private async Task<CommissionBreakdown> CalculateCommissionAsync(decimal itemsSubtotal)
    {
        // Get the active commission configuration
        var commissionConfig = await _context.CommissionConfigs
            .FirstOrDefaultAsync(c => c.IsActive);

        // If no configuration exists, create a default one
        if (commissionConfig == null)
        {
            commissionConfig = await GetOrCreateDefaultCommissionConfigAsync();
        }

        var breakdown = new CommissionBreakdown
        {
            CommissionRate = commissionConfig.CommissionPercentage,
            FixedCommission = commissionConfig.FixedCommissionAmount
        };

        // Calculate percentage-based commission
        breakdown.PercentageCommission = itemsSubtotal * (commissionConfig.CommissionPercentage / 100m);

        // Calculate total commission
        breakdown.TotalCommission = breakdown.PercentageCommission + breakdown.FixedCommission;

        // Calculate seller payout (items subtotal - commission)
        breakdown.SellerPayout = itemsSubtotal - breakdown.TotalCommission;

        _logger.LogDebug("Commission calculated: Rate={Rate}%, Fixed={Fixed}, Total={Total}, Payout={Payout}",
            breakdown.CommissionRate, breakdown.FixedCommission, breakdown.TotalCommission, breakdown.SellerPayout);

        return breakdown;
    }

    /// <summary>
    /// Gets or creates the default commission configuration.
    /// </summary>
    /// <returns>The commission configuration.</returns>
    private async Task<CommissionConfig> GetOrCreateDefaultCommissionConfigAsync()
    {
        var existingConfig = await _context.CommissionConfigs
            .FirstOrDefaultAsync();

        if (existingConfig != null)
        {
            return existingConfig;
        }

        // Create a default commission configuration
        var defaultConfig = new CommissionConfig
        {
            CommissionPercentage = 10.0m,
            FixedCommissionAmount = 0.50m,
            IsActive = true
        };

        _context.CommissionConfigs.Add(defaultConfig);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created default commission configuration");

        return defaultConfig;
    }
}
