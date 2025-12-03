using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for currency management service.
/// Handles CRUD operations for currencies and currency configuration.
/// </summary>
public interface ICurrencyService
{
    /// <summary>
    /// Gets all currencies, optionally filtered by enabled status.
    /// </summary>
    /// <param name="enabledOnly">If true, returns only enabled currencies.</param>
    /// <returns>A list of currencies ordered by display order.</returns>
    Task<List<Currency>> GetAllCurrenciesAsync(bool enabledOnly = false);

    /// <summary>
    /// Gets a currency by ID.
    /// </summary>
    /// <param name="id">The currency ID.</param>
    /// <returns>The currency or null if not found.</returns>
    Task<Currency?> GetCurrencyByIdAsync(int id);

    /// <summary>
    /// Gets a currency by code.
    /// </summary>
    /// <param name="code">The currency code (ISO 4217).</param>
    /// <returns>The currency or null if not found.</returns>
    Task<Currency?> GetCurrencyByCodeAsync(string code);

    /// <summary>
    /// Creates a new currency.
    /// </summary>
    /// <param name="currency">The currency to create.</param>
    /// <returns>The created currency.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a currency with the same code already exists.</exception>
    Task<Currency> CreateCurrencyAsync(Currency currency);

    /// <summary>
    /// Updates an existing currency.
    /// </summary>
    /// <param name="currency">The currency to update.</param>
    /// <returns>The updated currency.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the currency is not found.</exception>
    Task<Currency> UpdateCurrencyAsync(Currency currency);

    /// <summary>
    /// Enables a currency for use on the platform.
    /// </summary>
    /// <param name="id">The currency ID.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> EnableCurrencyAsync(int id);

    /// <summary>
    /// Disables a currency, making it unavailable for new listings and transactions.
    /// </summary>
    /// <param name="id">The currency ID.</param>
    /// <returns>True if successful, false otherwise.</returns>
    /// <exception cref="InvalidOperationException">Thrown if trying to disable the base currency.</exception>
    Task<bool> DisableCurrencyAsync(int id);

    /// <summary>
    /// Deletes a currency.
    /// </summary>
    /// <param name="id">The currency ID.</param>
    /// <returns>True if successful, false otherwise.</returns>
    /// <exception cref="InvalidOperationException">Thrown if trying to delete a currency that is in use or is the base currency.</exception>
    Task<bool> DeleteCurrencyAsync(int id);

    /// <summary>
    /// Updates the exchange rate for a currency.
    /// </summary>
    /// <param name="id">The currency ID.</param>
    /// <param name="exchangeRate">The new exchange rate.</param>
    /// <param name="source">The source of the exchange rate.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> UpdateExchangeRateAsync(int id, decimal exchangeRate, string source);

    /// <summary>
    /// Gets the current currency configuration.
    /// </summary>
    /// <returns>The currency configuration or a default configuration if none exists.</returns>
    Task<CurrencyConfig> GetCurrencyConfigAsync();

    /// <summary>
    /// Updates the currency configuration, including the base currency.
    /// </summary>
    /// <param name="config">The configuration to update.</param>
    /// <param name="currentUserId">The ID of the user making the change.</param>
    /// <returns>The updated configuration.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the base currency code is not valid or enabled.</exception>
    Task<CurrencyConfig> UpdateCurrencyConfigAsync(CurrencyConfig config, int currentUserId);

    /// <summary>
    /// Initializes default currencies if none exist.
    /// Creates common currencies like USD, EUR, GBP, etc.
    /// </summary>
    /// <returns>True if currencies were created, false if they already existed.</returns>
    Task<bool> InitializeDefaultCurrenciesAsync();

    /// <summary>
    /// Validates if a currency code exists and is enabled.
    /// </summary>
    /// <param name="code">The currency code to validate.</param>
    /// <returns>True if the currency exists and is enabled, false otherwise.</returns>
    Task<bool> IsCurrencyEnabledAsync(string code);
}
