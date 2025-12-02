using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a payout transaction to a seller.
/// Aggregates eligible escrow balances and tracks payment to seller's payout method.
/// </summary>
public class Payout
{
    /// <summary>
    /// Gets or sets the unique identifier for the payout.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the payout number (unique identifier for tracking).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string PayoutNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store ID this payout belongs to.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store this payout belongs to (navigation property).
    /// </summary>
    public Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the payout method ID used for this payout.
    /// </summary>
    public int? PayoutMethodId { get; set; }

    /// <summary>
    /// Gets or sets the payout method (navigation property).
    /// </summary>
    public PayoutMethod? PayoutMethod { get; set; }

    /// <summary>
    /// Gets or sets the payout schedule ID that triggered this payout.
    /// </summary>
    public int? PayoutScheduleId { get; set; }

    /// <summary>
    /// Gets or sets the payout schedule (navigation property).
    /// </summary>
    public PayoutSchedule? PayoutSchedule { get; set; }

    /// <summary>
    /// Gets or sets the total amount to be paid out to the seller.
    /// This is the sum of all eligible escrow net amounts.
    /// </summary>
    [Required]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency for this payout (ISO 4217 code).
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the current status of the payout.
    /// </summary>
    public PayoutStatus Status { get; set; } = PayoutStatus.Scheduled;

    /// <summary>
    /// Gets or sets the scheduled date for this payout.
    /// </summary>
    public DateTime ScheduledDate { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the payout was initiated.
    /// </summary>
    public DateTime? InitiatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the payout was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the payout failed.
    /// </summary>
    public DateTime? FailedAt { get; set; }

    /// <summary>
    /// Gets or sets the error message if the payout failed.
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the error reference code from the payment provider.
    /// This can be used to look up the error in the provider's system.
    /// </summary>
    [MaxLength(100)]
    public string? ErrorReference { get; set; }

    /// <summary>
    /// Gets or sets the number of retry attempts made for this payout.
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts allowed.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the next retry date for failed payouts.
    /// </summary>
    public DateTime? NextRetryDate { get; set; }

    /// <summary>
    /// Gets or sets the external transaction ID from the payment provider.
    /// </summary>
    [MaxLength(200)]
    public string? ExternalTransactionId { get; set; }

    /// <summary>
    /// Gets or sets notes about the payout (for audit purposes).
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the collection of escrow transactions included in this payout.
    /// </summary>
    public List<EscrowTransaction> EscrowTransactions { get; set; } = new();

    /// <summary>
    /// Gets or sets the date and time when the payout was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the payout was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
