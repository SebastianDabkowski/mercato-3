using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a monthly settlement report for a seller.
/// Aggregates financial activity for accounting and reconciliation.
/// </summary>
public class Settlement
{
    /// <summary>
    /// Gets or sets the unique identifier for the settlement.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the settlement number (unique identifier for tracking).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string SettlementNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the store ID this settlement belongs to.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store this settlement belongs to (navigation property).
    /// </summary>
    public Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the settlement period start date (inclusive).
    /// </summary>
    public DateTime PeriodStartDate { get; set; }

    /// <summary>
    /// Gets or sets the settlement period end date (inclusive).
    /// </summary>
    public DateTime PeriodEndDate { get; set; }

    /// <summary>
    /// Gets or sets the total gross sales amount for the period.
    /// </summary>
    public decimal GrossSales { get; set; }

    /// <summary>
    /// Gets or sets the total refunds amount for the period.
    /// </summary>
    public decimal Refunds { get; set; }

    /// <summary>
    /// Gets or sets the total commission amount deducted for the period.
    /// </summary>
    public decimal Commission { get; set; }

    /// <summary>
    /// Gets or sets the total adjustments amount (can be positive or negative).
    /// </summary>
    public decimal Adjustments { get; set; }

    /// <summary>
    /// Gets or sets the net amount payable to the seller.
    /// Calculated as: GrossSales - Refunds - Commission + Adjustments
    /// </summary>
    public decimal NetAmount { get; set; }

    /// <summary>
    /// Gets or sets the total amount paid out during this period.
    /// </summary>
    public decimal TotalPayouts { get; set; }

    /// <summary>
    /// Gets or sets the currency for this settlement (ISO 4217 code).
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the current status of the settlement.
    /// </summary>
    public SettlementStatus Status { get; set; } = SettlementStatus.Draft;

    /// <summary>
    /// Gets or sets the date and time when the settlement was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the settlement was finalized.
    /// </summary>
    public DateTime? FinalizedAt { get; set; }

    /// <summary>
    /// Gets or sets the version number for audit history.
    /// Incremented each time the settlement is regenerated.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether this is the current version of the settlement.
    /// </summary>
    public bool IsCurrentVersion { get; set; } = true;

    /// <summary>
    /// Gets or sets the previous settlement ID if this is a regeneration.
    /// </summary>
    public int? PreviousSettlementId { get; set; }

    /// <summary>
    /// Gets or sets the previous settlement (navigation property).
    /// </summary>
    public Settlement? PreviousSettlement { get; set; }

    /// <summary>
    /// Gets or sets notes about the settlement (for audit purposes).
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the collection of settlement items (order details).
    /// </summary>
    public List<SettlementItem> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of settlement adjustments.
    /// </summary>
    public List<SettlementAdjustment> SettlementAdjustments { get; set; } = new();

    /// <summary>
    /// Gets or sets the date and time when the settlement was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the settlement was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
