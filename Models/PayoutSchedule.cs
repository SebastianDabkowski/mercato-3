using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents the payout schedule configuration for a seller's store.
/// </summary>
public class PayoutSchedule
{
    /// <summary>
    /// Gets or sets the unique identifier for the payout schedule.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the store ID this payout schedule belongs to.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store this payout schedule belongs to (navigation property).
    /// </summary>
    public Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the payout frequency.
    /// </summary>
    public PayoutFrequency Frequency { get; set; } = PayoutFrequency.Weekly;

    /// <summary>
    /// Gets or sets the minimum balance threshold required to trigger a payout.
    /// Below this threshold, balances roll over to the next payout cycle.
    /// </summary>
    [Required]
    public decimal MinimumPayoutThreshold { get; set; } = 50.00m;

    /// <summary>
    /// Gets or sets the day of week for weekly payouts (0 = Sunday, 6 = Saturday).
    /// Only used when Frequency is Weekly or BiWeekly.
    /// </summary>
    public int? DayOfWeek { get; set; }

    /// <summary>
    /// Gets or sets the day of month for monthly payouts (1-28).
    /// Only used when Frequency is Monthly.
    /// </summary>
    public int? DayOfMonth { get; set; }

    /// <summary>
    /// Gets or sets the next scheduled payout date.
    /// </summary>
    public DateTime NextPayoutDate { get; set; }

    /// <summary>
    /// Gets or sets whether automatic payouts are enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the schedule was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the schedule was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
