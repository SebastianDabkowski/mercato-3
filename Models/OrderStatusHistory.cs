using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a history record of status changes for a seller sub-order.
/// Helps with support, dispute resolution, and order tracking.
/// </summary>
public class OrderStatusHistory
{
    /// <summary>
    /// Gets or sets the unique identifier for this history record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order ID that this history record belongs to.
    /// </summary>
    public int SellerSubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order (navigation property).
    /// </summary>
    public SellerSubOrder SellerSubOrder { get; set; } = null!;

    /// <summary>
    /// Gets or sets the previous status before the change (null for initial status).
    /// </summary>
    public OrderStatus? PreviousStatus { get; set; }

    /// <summary>
    /// Gets or sets the new status after the change.
    /// </summary>
    public OrderStatus NewStatus { get; set; }

    /// <summary>
    /// Gets or sets optional notes about the status change.
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the user ID who made the status change (null for system changes).
    /// </summary>
    public int? ChangedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user who made the change (navigation property).
    /// </summary>
    public User? ChangedByUser { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the status was changed.
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
