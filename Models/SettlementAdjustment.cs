using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents an adjustment to a settlement report.
/// Used for corrections, fees, credits, or previous month adjustments.
/// </summary>
public class SettlementAdjustment
{
    /// <summary>
    /// Gets or sets the unique identifier for the settlement adjustment.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the settlement ID this adjustment belongs to.
    /// </summary>
    public int SettlementId { get; set; }

    /// <summary>
    /// Gets or sets the settlement (navigation property).
    /// </summary>
    public Settlement Settlement { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of adjustment.
    /// </summary>
    public SettlementAdjustmentType Type { get; set; }

    /// <summary>
    /// Gets or sets the adjustment amount (positive for credits, negative for debits).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the description of the adjustment.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reference to the previous month's settlement if this is a prior period adjustment.
    /// </summary>
    public int? RelatedSettlementId { get; set; }

    /// <summary>
    /// Gets or sets the related settlement (navigation property).
    /// </summary>
    public Settlement? RelatedSettlement { get; set; }

    /// <summary>
    /// Gets or sets whether this is a prior period adjustment.
    /// </summary>
    public bool IsPriorPeriodAdjustment { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the adjustment was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID who created this adjustment.
    /// </summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the admin user who created this adjustment (navigation property).
    /// </summary>
    public User? CreatedByUser { get; set; }
}
