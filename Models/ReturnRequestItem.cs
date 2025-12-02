using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a specific item within a return request.
/// Used for partial returns where only some items from a sub-order are being returned.
/// </summary>
public class ReturnRequestItem
{
    /// <summary>
    /// Gets or sets the unique identifier for the return request item.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the return request ID that this item belongs to.
    /// </summary>
    public int ReturnRequestId { get; set; }

    /// <summary>
    /// Gets or sets the return request (navigation property).
    /// </summary>
    public ReturnRequest ReturnRequest { get; set; } = null!;

    /// <summary>
    /// Gets or sets the order item ID being returned.
    /// </summary>
    public int OrderItemId { get; set; }

    /// <summary>
    /// Gets or sets the order item (navigation property).
    /// </summary>
    public OrderItem OrderItem { get; set; } = null!;

    /// <summary>
    /// Gets or sets the quantity being returned (must be <= original quantity).
    /// </summary>
    [Required]
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the refund amount for this item (quantity * unit price).
    /// </summary>
    [Required]
    public decimal RefundAmount { get; set; }
}
