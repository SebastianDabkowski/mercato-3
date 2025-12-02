using MercatoApp.Models;
using MercatoApp.Data;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Interface for promo code operations.
/// </summary>
public interface IPromoCodeService
{
    /// <summary>
    /// Validates and retrieves a promo code by its code string.
    /// </summary>
    /// <param name="code">The promo code string.</param>
    /// <param name="userId">The user ID (optional for authenticated users).</param>
    /// <param name="sessionId">The session ID (optional for anonymous users).</param>
    /// <returns>The validated promo code or null if invalid/expired/ineligible.</returns>
    Task<PromoCode?> ValidatePromoCodeAsync(string code, int? userId, string? sessionId);

    /// <summary>
    /// Calculates the discount amount for a given promo code and cart.
    /// </summary>
    /// <param name="promoCode">The promo code to apply.</param>
    /// <param name="itemsBySeller">The cart items grouped by seller.</param>
    /// <param name="itemsSubtotal">The total items subtotal.</param>
    /// <returns>The calculated discount amount.</returns>
    decimal CalculateDiscount(PromoCode promoCode, Dictionary<Store, List<CartItem>> itemsBySeller, decimal itemsSubtotal);

    /// <summary>
    /// Increments the usage count for a promo code.
    /// This should be called when an order is successfully placed.
    /// </summary>
    /// <param name="promoCodeId">The promo code ID.</param>
    Task IncrementUsageCountAsync(int promoCodeId);
}

/// <summary>
/// Service for managing promo codes and discount calculations.
/// </summary>
public class PromoCodeService : IPromoCodeService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PromoCodeService> _logger;

    public PromoCodeService(
        ApplicationDbContext context,
        ILogger<PromoCodeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PromoCode?> ValidatePromoCodeAsync(string code, int? userId, string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            _logger.LogWarning("Empty promo code provided");
            return null;
        }

        // Normalize the code to uppercase for comparison
        var normalizedCode = code.Trim().ToUpper();

        // Find the promo code (case-insensitive via normalized comparison)
        var promoCode = await _context.PromoCodes
            .Include(p => p.Store)
            .FirstOrDefaultAsync(p => p.Code.ToUpper() == normalizedCode);

        if (promoCode == null)
        {
            _logger.LogWarning("Promo code not found: {Code}", code);
            return null;
        }

        // Check if active
        if (!promoCode.IsActive)
        {
            _logger.LogWarning("Promo code is inactive: {Code}", code);
            return null;
        }

        // Check if started
        if (promoCode.StartDate.HasValue && promoCode.StartDate.Value > DateTime.UtcNow)
        {
            _logger.LogWarning("Promo code not yet valid: {Code}, starts at {StartDate}", code, promoCode.StartDate.Value);
            return null;
        }

        // Check if expired
        if (promoCode.ExpirationDate.HasValue && promoCode.ExpirationDate.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("Promo code expired: {Code}, expired at {ExpirationDate}", code, promoCode.ExpirationDate.Value);
            return null;
        }

        // Check usage limit
        if (promoCode.MaximumUsageCount.HasValue && 
            promoCode.CurrentUsageCount >= promoCode.MaximumUsageCount.Value)
        {
            _logger.LogWarning("Promo code usage limit reached: {Code}, {CurrentUsage}/{MaxUsage}", 
                code, promoCode.CurrentUsageCount, promoCode.MaximumUsageCount.Value);
            return null;
        }

        _logger.LogInformation("Promo code validated successfully: {Code}", code);
        return promoCode;
    }

    /// <inheritdoc />
    public decimal CalculateDiscount(PromoCode promoCode, Dictionary<Store, List<CartItem>> itemsBySeller, decimal itemsSubtotal)
    {
        // For seller-specific promo codes, only apply to items from that seller
        decimal applicableSubtotal = itemsSubtotal;

        if (promoCode.Scope == PromoCodeScope.Seller && promoCode.StoreId.HasValue)
        {
            // Calculate subtotal only for items from the promo code's store
            var sellerItems = itemsBySeller
                .Where(kvp => kvp.Key.Id == promoCode.StoreId.Value)
                .SelectMany(kvp => kvp.Value);

            applicableSubtotal = sellerItems.Sum(item => item.PriceAtAdd * item.Quantity);

            _logger.LogDebug("Seller-specific promo code: applicable subtotal {ApplicableSubtotal} from store {StoreId}",
                applicableSubtotal, promoCode.StoreId.Value);
        }

        // Check minimum order requirement
        if (promoCode.MinimumOrderSubtotal.HasValue && applicableSubtotal < promoCode.MinimumOrderSubtotal.Value)
        {
            _logger.LogWarning("Order subtotal {Subtotal} does not meet minimum requirement {MinimumSubtotal}",
                applicableSubtotal, promoCode.MinimumOrderSubtotal.Value);
            return 0;
        }

        // Calculate discount based on type
        decimal discount = 0;

        if (promoCode.DiscountType == PromoCodeDiscountType.Percentage)
        {
            discount = applicableSubtotal * (promoCode.DiscountValue / 100m);
            
            // Apply maximum discount cap if specified
            if (promoCode.MaximumDiscountAmount.HasValue && discount > promoCode.MaximumDiscountAmount.Value)
            {
                discount = promoCode.MaximumDiscountAmount.Value;
                _logger.LogDebug("Discount capped at maximum amount: {MaxDiscount}", discount);
            }
        }
        else if (promoCode.DiscountType == PromoCodeDiscountType.FixedAmount)
        {
            discount = promoCode.DiscountValue;

            // Don't discount more than the applicable subtotal
            if (discount > applicableSubtotal)
            {
                discount = applicableSubtotal;
                _logger.LogDebug("Discount capped at applicable subtotal: {Discount}", discount);
            }
        }

        _logger.LogInformation("Calculated discount: {Discount} for promo code {Code}", discount, promoCode.Code);
        return discount;
    }

    /// <inheritdoc />
    public async Task IncrementUsageCountAsync(int promoCodeId)
    {
        // Use ExecuteUpdateAsync for atomic increment to avoid race conditions
        await _context.PromoCodes
            .Where(p => p.Id == promoCodeId)
            .ExecuteUpdateAsync(p => p.SetProperty(
                pc => pc.CurrentUsageCount,
                pc => pc.CurrentUsageCount + 1));

        _logger.LogInformation("Incremented usage count for promo code {PromoCodeId}", promoCodeId);
    }
}
