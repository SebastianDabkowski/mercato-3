using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a payment transaction for an order.
/// Tracks the payment flow through the payment provider.
/// </summary>
public class PaymentTransaction
{
    /// <summary>
    /// Gets or sets the unique identifier for the transaction.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the order ID this transaction is for.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Gets or sets the order (navigation property).
    /// </summary>
    public Order Order { get; set; } = null!;

    /// <summary>
    /// Gets or sets the payment method ID used for this transaction.
    /// </summary>
    public int PaymentMethodId { get; set; }

    /// <summary>
    /// Gets or sets the payment method (navigation property).
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction reference from the payment provider.
    /// </summary>
    [MaxLength(200)]
    public string? ProviderTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the idempotency key to prevent duplicate transactions.
    /// Used to ensure payment provider retries don't create duplicate charges.
    /// </summary>
    [MaxLength(100)]
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Gets or sets the payment amount.
    /// </summary>
    [Required]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency code (e.g., "USD", "EUR").
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the payment status.
    /// </summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// Gets or sets the error message if payment failed.
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata from the payment provider (JSON format).
    /// </summary>
    public string? ProviderMetadata { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the transaction was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the transaction was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the payment was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}
