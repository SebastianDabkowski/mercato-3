using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a commission transaction record for audit trail and reporting.
/// Tracks commission calculations at payment confirmation and refund adjustments.
/// </summary>
public class CommissionTransaction
{
    /// <summary>
    /// Gets or sets the unique identifier for the commission transaction.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the escrow transaction ID this commission is associated with.
    /// </summary>
    public int EscrowTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the escrow transaction (navigation property).
    /// </summary>
    public EscrowTransaction EscrowTransaction { get; set; } = null!;

    /// <summary>
    /// Gets or sets the store ID (seller) this commission is charged to.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store (navigation property).
    /// </summary>
    public Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the category ID if category-specific commission was applied.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the category (navigation property).
    /// </summary>
    public Category? Category { get; set; }

    /// <summary>
    /// Gets or sets the commission type (Initial or Refund Adjustment).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TransactionType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the gross amount this commission was calculated from.
    /// </summary>
    [Required]
    public decimal GrossAmount { get; set; }

    /// <summary>
    /// Gets or sets the commission percentage applied.
    /// Stored for historical tracking even if config changes.
    /// </summary>
    [Required]
    public decimal CommissionPercentage { get; set; }

    /// <summary>
    /// Gets or sets the fixed commission amount applied.
    /// Stored for historical tracking even if config changes.
    /// </summary>
    [Required]
    public decimal FixedCommissionAmount { get; set; }

    /// <summary>
    /// Gets or sets the calculated commission amount.
    /// </summary>
    [Required]
    public decimal CommissionAmount { get; set; }

    /// <summary>
    /// Gets or sets the commission rule source (Global, Seller, or Category).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string CommissionSource { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets notes about this commission calculation.
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the commission transaction was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Commission transaction types.
/// </summary>
public static class CommissionTransactionType
{
    /// <summary>
    /// Initial commission calculation at payment confirmation.
    /// </summary>
    public const string Initial = "Initial";

    /// <summary>
    /// Commission adjustment due to partial refund.
    /// </summary>
    public const string RefundAdjustment = "RefundAdjustment";
}

/// <summary>
/// Commission rule sources.
/// </summary>
public static class CommissionSource
{
    /// <summary>
    /// Global platform commission configuration.
    /// </summary>
    public const string Global = "Global";

    /// <summary>
    /// Seller-specific commission override.
    /// </summary>
    public const string Seller = "Seller";

    /// <summary>
    /// Category-specific commission override.
    /// </summary>
    public const string Category = "Category";
}
