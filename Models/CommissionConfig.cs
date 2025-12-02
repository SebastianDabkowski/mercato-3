using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents the platform-wide commission configuration.
/// Commissions are calculated internally and deducted from seller payouts.
/// Buyers do not see commission amounts.
/// </summary>
public class CommissionConfig
{
    /// <summary>
    /// Gets or sets the unique identifier for the commission configuration.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the commission rate as a percentage (e.g., 10.5 for 10.5%).
    /// Applied to the item subtotal before shipping.
    /// </summary>
    [Required]
    [Range(0, 100)]
    public decimal CommissionPercentage { get; set; }

    /// <summary>
    /// Gets or sets the fixed commission amount per transaction.
    /// Applied in addition to the percentage-based commission.
    /// </summary>
    [Range(0, 999999.99)]
    public decimal FixedCommissionAmount { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether this configuration is currently active.
    /// Only one configuration should be active at a time.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the configuration was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
