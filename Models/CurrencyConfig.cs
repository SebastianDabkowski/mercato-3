using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents the platform-wide currency configuration settings.
/// Stores the base currency and related settings.
/// </summary>
public class CurrencyConfig
{
    /// <summary>
    /// Gets or sets the unique identifier for the configuration.
    /// Typically there is only one active configuration record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the base currency code (ISO 4217 code).
    /// All other currencies' exchange rates are relative to this base currency.
    /// Examples: "USD", "EUR"
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string BaseCurrencyCode { get; set; } = "USD";

    /// <summary>
    /// Gets or sets whether to automatically update exchange rates.
    /// This is a placeholder for future integration with exchange rate services.
    /// </summary>
    public bool AutoUpdateExchangeRates { get; set; } = false;

    /// <summary>
    /// Gets or sets the frequency of automatic exchange rate updates in hours.
    /// Only relevant if AutoUpdateExchangeRates is true.
    /// </summary>
    [Range(1, 168)]
    public int UpdateFrequencyHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets the date and time when the configuration was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user ID of the admin who last updated this configuration.
    /// </summary>
    public int? UpdatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user who last updated this configuration (navigation property).
    /// </summary>
    public User? UpdatedByUser { get; set; }

    /// <summary>
    /// Gets or sets notes about the last configuration change.
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }
}
