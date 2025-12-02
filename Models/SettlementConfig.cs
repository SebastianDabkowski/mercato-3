using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents the settlement configuration for the platform.
/// Controls when and how monthly settlements are generated.
/// </summary>
public class SettlementConfig
{
    /// <summary>
    /// Gets or sets the unique identifier for the settlement configuration.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the day of month when settlements should be generated (1-28).
    /// </summary>
    [Range(1, 28)]
    public int GenerationDayOfMonth { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether settlements should be auto-generated.
    /// </summary>
    public bool AutoGenerateEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of days after period end to include orders (grace period).
    /// For example, 3 means orders placed up to 3 days into the next month are included in the previous month.
    /// </summary>
    [Range(0, 7)]
    public int GracePeriodDays { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether to use calendar month (true) or rolling 30-day periods (false).
    /// </summary>
    public bool UseCalendarMonth { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the configuration was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
