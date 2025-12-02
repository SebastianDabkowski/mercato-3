using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents the shipping method selected for a specific seller in an order.
/// Since orders can contain items from multiple sellers, each seller can have a different shipping method.
/// </summary>
public class OrderShippingMethod
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the order ID.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Gets or sets the order (navigation property).
    /// </summary>
    public Order Order { get; set; } = null!;

    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store (navigation property).
    /// </summary>
    public Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the shipping method ID selected for this seller.
    /// </summary>
    public int ShippingMethodId { get; set; }

    /// <summary>
    /// Gets or sets the shipping method (navigation property).
    /// </summary>
    public ShippingMethod ShippingMethod { get; set; } = null!;

    /// <summary>
    /// Gets or sets the calculated shipping cost for this seller.
    /// </summary>
    [Required]
    public decimal ShippingCost { get; set; }
}
