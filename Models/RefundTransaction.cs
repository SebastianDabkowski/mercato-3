using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a refund transaction for a payment.
/// Tracks the complete audit trail of refund operations.
/// </summary>
public class RefundTransaction
{
    /// <summary>
    /// Gets or sets the unique identifier for the refund transaction.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the refund reference number (human-readable).
    /// Format: REF-{Timestamp}-{Id} (e.g., "REF-20241202-12345")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string RefundNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order ID that this refund is for.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Gets or sets the order (navigation property).
    /// </summary>
    public Order Order { get; set; } = null!;

    /// <summary>
    /// Gets or sets the original payment transaction ID being refunded.
    /// </summary>
    public int PaymentTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the payment transaction (navigation property).
    /// </summary>
    public PaymentTransaction PaymentTransaction { get; set; } = null!;

    /// <summary>
    /// Gets or sets the seller sub-order ID if this is a partial refund for a specific seller.
    /// Null for full order refunds.
    /// </summary>
    public int? SellerSubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order (navigation property).
    /// </summary>
    public SellerSubOrder? SellerSubOrder { get; set; }

    /// <summary>
    /// Gets or sets the return request ID if this refund is associated with a return/complaint.
    /// Null for refunds not initiated by return requests (e.g., order cancellations).
    /// </summary>
    public int? ReturnRequestId { get; set; }

    /// <summary>
    /// Gets or sets the return request (navigation property).
    /// </summary>
    public ReturnRequest? ReturnRequest { get; set; }

    /// <summary>
    /// Gets or sets the type of refund (Full or Partial).
    /// </summary>
    public RefundType RefundType { get; set; }

    /// <summary>
    /// Gets or sets the refund amount.
    /// </summary>
    [Required]
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Gets or sets the currency code (e.g., "USD", "EUR").
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the current status of the refund.
    /// </summary>
    public RefundStatus Status { get; set; } = RefundStatus.Requested;

    /// <summary>
    /// Gets or sets the reason for the refund.
    /// </summary>
    [MaxLength(1000)]
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the user ID who initiated the refund (admin or seller).
    /// </summary>
    public int InitiatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user who initiated the refund (navigation property).
    /// </summary>
    public User InitiatedBy { get; set; } = null!;

    /// <summary>
    /// Gets or sets the payment provider's refund transaction ID.
    /// </summary>
    [MaxLength(200)]
    public string? ProviderRefundId { get; set; }

    /// <summary>
    /// Gets or sets the error message if refund failed.
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata from the payment provider (JSON format).
    /// </summary>
    public string? ProviderMetadata { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the refund was requested.
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the refund was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the refund was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets notes about the refund (for audit purposes).
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }
}
