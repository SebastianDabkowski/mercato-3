using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents an escrow allocation for a seller from a buyer's payment.
/// Each seller in a multi-vendor order gets their own escrow transaction.
/// </summary>
public class EscrowTransaction
{
    /// <summary>
    /// Gets or sets the unique identifier for the escrow transaction.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the payment transaction ID this escrow is derived from.
    /// </summary>
    public int PaymentTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the payment transaction (navigation property).
    /// </summary>
    public PaymentTransaction PaymentTransaction { get; set; } = null!;

    /// <summary>
    /// Gets or sets the seller sub-order ID this escrow is allocated to.
    /// </summary>
    public int SellerSubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order (navigation property).
    /// </summary>
    public SellerSubOrder SellerSubOrder { get; set; } = null!;

    /// <summary>
    /// Gets or sets the store ID (seller) this escrow belongs to.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store (navigation property).
    /// </summary>
    public Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the gross amount allocated to this seller (subtotal + shipping).
    /// </summary>
    [Required]
    public decimal GrossAmount { get; set; }

    /// <summary>
    /// Gets or sets the platform commission amount deducted from this seller's payment.
    /// Calculated based on active CommissionConfig.
    /// </summary>
    [Required]
    public decimal CommissionAmount { get; set; }

    /// <summary>
    /// Gets or sets the net amount eligible for payout to the seller (gross - commission).
    /// </summary>
    [Required]
    public decimal NetAmount { get; set; }

    /// <summary>
    /// Gets or sets the current status of the escrow.
    /// </summary>
    public EscrowStatus Status { get; set; } = EscrowStatus.Held;

    /// <summary>
    /// Gets or sets the date and time when the escrow becomes eligible for payout.
    /// Configurable based on marketplace policy (e.g., 7 days after delivery).
    /// </summary>
    public DateTime? EligibleForPayoutAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the escrow was released to the seller.
    /// </summary>
    public DateTime? ReleasedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the escrow was returned to the buyer.
    /// </summary>
    public DateTime? ReturnedToBuyerAt { get; set; }

    /// <summary>
    /// Gets or sets the amount that has been refunded from this escrow.
    /// Used for partial refunds in case of partial returns.
    /// </summary>
    public decimal RefundedAmount { get; set; } = 0;

    /// <summary>
    /// Gets or sets notes about the escrow transaction (for audit purposes).
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the escrow transaction was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the escrow transaction was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
