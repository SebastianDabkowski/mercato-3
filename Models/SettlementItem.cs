using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents an individual order item in a settlement report.
/// </summary>
public class SettlementItem
{
    /// <summary>
    /// Gets or sets the unique identifier for the settlement item.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the settlement ID this item belongs to.
    /// </summary>
    public int SettlementId { get; set; }

    /// <summary>
    /// Gets or sets the settlement (navigation property).
    /// </summary>
    public Settlement Settlement { get; set; } = null!;

    /// <summary>
    /// Gets or sets the order ID.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Gets or sets the order (navigation property).
    /// </summary>
    public Order Order { get; set; } = null!;

    /// <summary>
    /// Gets or sets the seller sub-order ID.
    /// </summary>
    public int? SellerSubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order (navigation property).
    /// </summary>
    public SellerSubOrder? SellerSubOrder { get; set; }

    /// <summary>
    /// Gets or sets the escrow transaction ID.
    /// </summary>
    public int? EscrowTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the escrow transaction (navigation property).
    /// </summary>
    public EscrowTransaction? EscrowTransaction { get; set; }

    /// <summary>
    /// Gets or sets the order number for display.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order date.
    /// </summary>
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Gets or sets the gross amount for this item.
    /// </summary>
    public decimal GrossAmount { get; set; }

    /// <summary>
    /// Gets or sets the refund amount for this item.
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Gets or sets the commission amount for this item.
    /// </summary>
    public decimal CommissionAmount { get; set; }

    /// <summary>
    /// Gets or sets the net amount for this item.
    /// </summary>
    public decimal NetAmount { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the item was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
