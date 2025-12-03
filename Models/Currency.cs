using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a currency that can be used for transactions on the platform.
/// Currencies can be enabled or disabled, and exchange rate information is tracked.
/// </summary>
public class Currency
{
    /// <summary>
    /// Gets or sets the unique identifier for the currency.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the currency code (ISO 4217 code).
    /// Examples: "USD", "EUR", "GBP", "JPY"
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the currency.
    /// Examples: "US Dollar", "Euro", "British Pound"
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the currency symbol.
    /// Examples: "$", "€", "£", "¥"
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of decimal places for this currency.
    /// Most currencies use 2, but some (like JPY) use 0.
    /// </summary>
    [Range(0, 4)]
    public int DecimalPlaces { get; set; } = 2;

    /// <summary>
    /// Gets or sets whether this currency is enabled for use on the platform.
    /// Disabled currencies are not available for new listings or transactions.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the exchange rate relative to the base currency.
    /// This is for display purposes; actual exchange is handled by another module.
    /// A value of 1.0 typically represents the base currency.
    /// </summary>
    public decimal ExchangeRate { get; set; } = 1.0m;

    /// <summary>
    /// Gets or sets the source of the exchange rate.
    /// Examples: "Manual", "ECB", "OpenExchangeRates", "XE.com"
    /// </summary>
    [MaxLength(100)]
    public string? ExchangeRateSource { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the exchange rate was last updated.
    /// </summary>
    public DateTime? ExchangeRateLastUpdated { get; set; }

    /// <summary>
    /// Gets or sets the display order for this currency in lists.
    /// Lower values appear first.
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Gets or sets the date and time when the currency was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the currency was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
