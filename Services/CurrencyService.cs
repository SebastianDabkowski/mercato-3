using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing currencies and currency configuration.
/// </summary>
public class CurrencyService : ICurrencyService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CurrencyService> _logger;

    public CurrencyService(
        ApplicationDbContext context,
        ILogger<CurrencyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<Currency>> GetAllCurrenciesAsync(bool enabledOnly = false)
    {
        var query = _context.Currencies.AsQueryable();

        if (enabledOnly)
        {
            query = query.Where(c => c.IsEnabled);
        }

        return await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Code)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Currency?> GetCurrencyByIdAsync(int id)
    {
        return await _context.Currencies.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<Currency?> GetCurrencyByCodeAsync(string code)
    {
        return await _context.Currencies
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());
    }

    /// <inheritdoc />
    public async Task<Currency> CreateCurrencyAsync(Currency currency)
    {
        // Validate that currency code is unique
        var existing = await GetCurrencyByCodeAsync(currency.Code);
        if (existing != null)
        {
            throw new InvalidOperationException($"Currency with code {currency.Code} already exists.");
        }

        currency.Code = currency.Code.ToUpper();
        currency.CreatedAt = DateTime.UtcNow;
        currency.UpdatedAt = DateTime.UtcNow;

        _context.Currencies.Add(currency);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created currency {Code} ({Name})", currency.Code, currency.Name);

        return currency;
    }

    /// <inheritdoc />
    public async Task<Currency> UpdateCurrencyAsync(Currency currency)
    {
        var existing = await GetCurrencyByIdAsync(currency.Id);
        if (existing == null)
        {
            throw new InvalidOperationException($"Currency with ID {currency.Id} not found.");
        }

        existing.Name = currency.Name;
        existing.Symbol = currency.Symbol;
        existing.DecimalPlaces = currency.DecimalPlaces;
        existing.IsEnabled = currency.IsEnabled;
        existing.ExchangeRate = currency.ExchangeRate;
        existing.ExchangeRateSource = currency.ExchangeRateSource;
        existing.ExchangeRateLastUpdated = currency.ExchangeRateLastUpdated;
        existing.DisplayOrder = currency.DisplayOrder;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated currency {Code}", existing.Code);

        return existing;
    }

    /// <inheritdoc />
    public async Task<bool> EnableCurrencyAsync(int id)
    {
        var currency = await GetCurrencyByIdAsync(id);
        if (currency == null)
        {
            return false;
        }

        if (currency.IsEnabled)
        {
            return true; // Already enabled
        }

        currency.IsEnabled = true;
        currency.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Enabled currency {Code}", currency.Code);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DisableCurrencyAsync(int id)
    {
        var currency = await GetCurrencyByIdAsync(id);
        if (currency == null)
        {
            return false;
        }

        // Check if this is the base currency
        var config = await GetCurrencyConfigAsync();
        if (currency.Code == config.BaseCurrencyCode)
        {
            throw new InvalidOperationException("Cannot disable the base currency. Please change the base currency first.");
        }

        if (!currency.IsEnabled)
        {
            return true; // Already disabled
        }

        currency.IsEnabled = false;
        currency.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Disabled currency {Code}", currency.Code);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteCurrencyAsync(int id)
    {
        var currency = await GetCurrencyByIdAsync(id);
        if (currency == null)
        {
            return false;
        }

        // Check if this is the base currency
        var config = await GetCurrencyConfigAsync();
        if (currency.Code == config.BaseCurrencyCode)
        {
            throw new InvalidOperationException("Cannot delete the base currency. Please change the base currency first.");
        }

        // In a real application, check if the currency is used in any transactions
        // For now, we'll allow deletion

        _context.Currencies.Remove(currency);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted currency {Code}", currency.Code);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateExchangeRateAsync(int id, decimal exchangeRate, string source)
    {
        var currency = await GetCurrencyByIdAsync(id);
        if (currency == null)
        {
            return false;
        }

        currency.ExchangeRate = exchangeRate;
        currency.ExchangeRateSource = source;
        currency.ExchangeRateLastUpdated = DateTime.UtcNow;
        currency.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated exchange rate for {Code} to {Rate} from {Source}", 
            currency.Code, exchangeRate, source);

        return true;
    }

    /// <inheritdoc />
    public async Task<CurrencyConfig> GetCurrencyConfigAsync()
    {
        var config = await _context.CurrencyConfigs.FirstOrDefaultAsync();
        
        if (config == null)
        {
            // Create default configuration
            config = new CurrencyConfig
            {
                BaseCurrencyCode = "USD",
                AutoUpdateExchangeRates = false,
                UpdateFrequencyHours = 24,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CurrencyConfigs.Add(config);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created default currency configuration with base currency USD");
        }

        return config;
    }

    /// <inheritdoc />
    public async Task<CurrencyConfig> UpdateCurrencyConfigAsync(CurrencyConfig config, int currentUserId)
    {
        var existing = await GetCurrencyConfigAsync();

        // Validate that the base currency exists and is enabled
        var baseCurrency = await GetCurrencyByCodeAsync(config.BaseCurrencyCode);
        if (baseCurrency == null)
        {
            throw new InvalidOperationException($"Currency with code {config.BaseCurrencyCode} does not exist.");
        }

        if (!baseCurrency.IsEnabled)
        {
            throw new InvalidOperationException($"Currency {config.BaseCurrencyCode} must be enabled to be set as the base currency.");
        }

        var oldBaseCurrency = existing.BaseCurrencyCode;
        existing.BaseCurrencyCode = config.BaseCurrencyCode.ToUpper();
        existing.AutoUpdateExchangeRates = config.AutoUpdateExchangeRates;
        existing.UpdateFrequencyHours = config.UpdateFrequencyHours;
        existing.UpdatedByUserId = currentUserId;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.Notes = config.Notes;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated currency configuration. Base currency changed from {Old} to {New} by user {UserId}", 
            oldBaseCurrency, existing.BaseCurrencyCode, currentUserId);

        return existing;
    }

    /// <inheritdoc />
    public async Task<bool> InitializeDefaultCurrenciesAsync()
    {
        var existingCount = await _context.Currencies.CountAsync();
        if (existingCount > 0)
        {
            return false; // Currencies already exist
        }

        var defaultCurrencies = new List<Currency>
        {
            new Currency
            {
                Code = "USD",
                Name = "US Dollar",
                Symbol = "$",
                DecimalPlaces = 2,
                IsEnabled = true,
                ExchangeRate = 1.0m,
                ExchangeRateSource = "Base Currency",
                ExchangeRateLastUpdated = DateTime.UtcNow,
                DisplayOrder = 1
            },
            new Currency
            {
                Code = "EUR",
                Name = "Euro",
                Symbol = "€",
                DecimalPlaces = 2,
                IsEnabled = true,
                ExchangeRate = 0.92m,
                ExchangeRateSource = "Manual",
                ExchangeRateLastUpdated = DateTime.UtcNow,
                DisplayOrder = 2
            },
            new Currency
            {
                Code = "GBP",
                Name = "British Pound",
                Symbol = "£",
                DecimalPlaces = 2,
                IsEnabled = true,
                ExchangeRate = 0.79m,
                ExchangeRateSource = "Manual",
                ExchangeRateLastUpdated = DateTime.UtcNow,
                DisplayOrder = 3
            },
            new Currency
            {
                Code = "JPY",
                Name = "Japanese Yen",
                Symbol = "¥",
                DecimalPlaces = 0,
                IsEnabled = true,
                ExchangeRate = 149.5m,
                ExchangeRateSource = "Manual",
                ExchangeRateLastUpdated = DateTime.UtcNow,
                DisplayOrder = 4
            },
            new Currency
            {
                Code = "CAD",
                Name = "Canadian Dollar",
                Symbol = "CA$",
                DecimalPlaces = 2,
                IsEnabled = true,
                ExchangeRate = 1.36m,
                ExchangeRateSource = "Manual",
                ExchangeRateLastUpdated = DateTime.UtcNow,
                DisplayOrder = 5
            },
            new Currency
            {
                Code = "AUD",
                Name = "Australian Dollar",
                Symbol = "A$",
                DecimalPlaces = 2,
                IsEnabled = true,
                ExchangeRate = 1.53m,
                ExchangeRateSource = "Manual",
                ExchangeRateLastUpdated = DateTime.UtcNow,
                DisplayOrder = 6
            }
        };

        _context.Currencies.AddRange(defaultCurrencies);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Initialized {Count} default currencies", defaultCurrencies.Count);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> IsCurrencyEnabledAsync(string code)
    {
        var currency = await GetCurrencyByCodeAsync(code);
        return currency?.IsEnabled ?? false;
    }
}
