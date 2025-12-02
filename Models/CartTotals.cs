namespace MercatoApp.Models;

/// <summary>
/// Represents the calculated totals for a shopping cart.
/// Includes item subtotal, shipping costs, and total amount payable.
/// </summary>
public class CartTotals
{
    /// <summary>
    /// Gets or sets the subtotal of all items in the cart (price × quantity).
    /// </summary>
    public decimal ItemsSubtotal { get; set; }

    /// <summary>
    /// Gets or sets the total shipping cost aggregated from all sellers.
    /// </summary>
    public decimal TotalShipping { get; set; }

    /// <summary>
    /// Gets or sets the total amount payable by the buyer (items + shipping).
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the shipping breakdown by seller.
    /// </summary>
    public List<SellerShippingBreakdown> ShippingBreakdown { get; set; } = new();

    /// <summary>
    /// Gets or sets the internal commission breakdown (not shown to buyers).
    /// This is populated only for internal/admin views.
    /// </summary>
    public CommissionBreakdown? InternalCommission { get; set; }
}

/// <summary>
/// Represents shipping cost breakdown for a single seller.
/// </summary>
public class SellerShippingBreakdown
{
    /// <summary>
    /// Gets or sets the store for this shipping calculation.
    /// </summary>
    public Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the subtotal for items from this seller.
    /// </summary>
    public decimal ItemsSubtotal { get; set; }

    /// <summary>
    /// Gets or sets the number of items from this seller.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets or sets the shipping cost for items from this seller.
    /// </summary>
    public decimal ShippingCost { get; set; }

    /// <summary>
    /// Gets or sets whether free shipping was applied.
    /// </summary>
    public bool IsFreeShipping { get; set; }

    /// <summary>
    /// Gets or sets the shipping rule used for calculation (if any).
    /// </summary>
    public ShippingRule? AppliedShippingRule { get; set; }
}

/// <summary>
/// Represents internal commission breakdown (not visible to buyers).
/// Used for internal financial calculations and seller payouts.
/// </summary>
public class CommissionBreakdown
{
    /// <summary>
    /// Gets or sets the total commission amount.
    /// </summary>
    public decimal TotalCommission { get; set; }

    /// <summary>
    /// Gets or sets the percentage-based commission.
    /// </summary>
    public decimal PercentageCommission { get; set; }

    /// <summary>
    /// Gets or sets the fixed commission amount.
    /// </summary>
    public decimal FixedCommission { get; set; }

    /// <summary>
    /// Gets or sets the commission rate applied.
    /// </summary>
    public decimal CommissionRate { get; set; }

    /// <summary>
    /// Gets or sets the net amount payable to sellers (after commission).
    /// </summary>
    public decimal SellerPayout { get; set; }
}
