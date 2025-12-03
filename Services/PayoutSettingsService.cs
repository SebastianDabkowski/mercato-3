using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Result of a payout settings operation.
/// </summary>
public class PayoutSettingsResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public PayoutMethod? PayoutMethod { get; set; }
}

/// <summary>
/// Data for creating or updating a bank transfer payout method.
/// </summary>
public class BankTransferPayoutData
{
    public required string DisplayName { get; set; }
    public required string BankName { get; set; }
    public required string BankAccountHolderName { get; set; }
    public required string BankAccountNumber { get; set; }
    public string? BankRoutingNumber { get; set; }
    public string? Currency { get; set; }
    public string? CountryCode { get; set; }
    public bool IsDefault { get; set; }
}

/// <summary>
/// Summary of payout settings status for a store.
/// </summary>
public class PayoutSettingsSummary
{
    public bool HasPayoutMethods { get; set; }
    public bool HasDefaultMethod { get; set; }
    public bool HasVerifiedMethod { get; set; }
    public bool IsPayoutConfigurationComplete { get; set; }
    public PayoutMethod? DefaultPayoutMethod { get; set; }
    public List<PayoutMethod> AllPayoutMethods { get; set; } = new();
    public List<string> ConfigurationIssues { get; set; } = new();
}

/// <summary>
/// Interface for payout settings service.
/// </summary>
public interface IPayoutSettingsService
{
    /// <summary>
    /// Gets all payout methods for a store.
    /// </summary>
    Task<List<PayoutMethod>> GetPayoutMethodsAsync(int storeId);

    /// <summary>
    /// Gets a specific payout method by ID.
    /// </summary>
    Task<PayoutMethod?> GetPayoutMethodAsync(int payoutMethodId, int storeId);

    /// <summary>
    /// Gets the default payout method for a store.
    /// </summary>
    Task<PayoutMethod?> GetDefaultPayoutMethodAsync(int storeId);

    /// <summary>
    /// Adds a new bank transfer payout method.
    /// </summary>
    Task<PayoutSettingsResult> AddBankTransferPayoutMethodAsync(int storeId, BankTransferPayoutData data);

    /// <summary>
    /// Updates an existing bank transfer payout method.
    /// </summary>
    Task<PayoutSettingsResult> UpdateBankTransferPayoutMethodAsync(int payoutMethodId, int storeId, BankTransferPayoutData data);

    /// <summary>
    /// Deletes a payout method.
    /// </summary>
    Task<PayoutSettingsResult> DeletePayoutMethodAsync(int payoutMethodId, int storeId);

    /// <summary>
    /// Sets a payout method as the default.
    /// </summary>
    Task<PayoutSettingsResult> SetDefaultPayoutMethodAsync(int payoutMethodId, int storeId);

    /// <summary>
    /// Gets the payout settings summary for a store.
    /// </summary>
    Task<PayoutSettingsSummary> GetPayoutSettingsSummaryAsync(int storeId);

    /// <summary>
    /// Checks if the payout configuration is complete for a store.
    /// </summary>
    Task<bool> IsPayoutConfigurationCompleteAsync(int storeId);
}

/// <summary>
/// Service for managing seller payout settings.
/// </summary>
public class PayoutSettingsService : IPayoutSettingsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PayoutSettingsService> _logger;
    private readonly IDataEncryptionService _encryptionService;

    public PayoutSettingsService(
        ApplicationDbContext context,
        ILogger<PayoutSettingsService> logger,
        IDataEncryptionService encryptionService)
    {
        _context = context;
        _logger = logger;
        _encryptionService = encryptionService;
    }

    /// <inheritdoc />
    public async Task<List<PayoutMethod>> GetPayoutMethodsAsync(int storeId)
    {
        return await _context.PayoutMethods
            .Where(p => p.StoreId == storeId)
            .OrderByDescending(p => p.IsDefault)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<PayoutMethod?> GetPayoutMethodAsync(int payoutMethodId, int storeId)
    {
        return await _context.PayoutMethods
            .FirstOrDefaultAsync(p => p.Id == payoutMethodId && p.StoreId == storeId);
    }

    /// <inheritdoc />
    public async Task<PayoutMethod?> GetDefaultPayoutMethodAsync(int storeId)
    {
        return await _context.PayoutMethods
            .FirstOrDefaultAsync(p => p.StoreId == storeId && p.IsDefault);
    }

    /// <inheritdoc />
    public async Task<PayoutSettingsResult> AddBankTransferPayoutMethodAsync(int storeId, BankTransferPayoutData data)
    {
        var result = new PayoutSettingsResult();

        // Validate required fields
        var validationErrors = ValidateBankTransferData(data);
        if (validationErrors.Count > 0)
        {
            result.Errors.AddRange(validationErrors);
            return result;
        }

        // Verify store exists
        var storeExists = await _context.Stores.AnyAsync(s => s.Id == storeId);
        if (!storeExists)
        {
            result.Errors.Add("Store not found.");
            return result;
        }

        // If this is set as default, remove default from other methods
        if (data.IsDefault)
        {
            await ClearDefaultPayoutMethodAsync(storeId);
        }

        // Check if this is the first payout method - make it default automatically
        var hasExistingMethods = await _context.PayoutMethods.AnyAsync(p => p.StoreId == storeId);
        var isDefault = data.IsDefault || !hasExistingMethods;

        // Create the payout method
        var payoutMethod = new PayoutMethod
        {
            StoreId = storeId,
            MethodType = PayoutMethodType.BankTransfer,
            DisplayName = data.DisplayName.Trim(),
            BankName = data.BankName.Trim(),
            BankAccountHolderName = data.BankAccountHolderName.Trim(),
            BankAccountNumberEncrypted = _encryptionService.Encrypt(data.BankAccountNumber.Trim()),
            BankAccountNumberLast4 = GetLast4Digits(data.BankAccountNumber.Trim()),
            BankRoutingNumber = data.BankRoutingNumber?.Trim(),
            Currency = string.IsNullOrWhiteSpace(data.Currency) ? null : data.Currency.Trim().ToUpperInvariant(),
            CountryCode = string.IsNullOrWhiteSpace(data.CountryCode) ? null : data.CountryCode.Trim().ToUpperInvariant(),
            IsDefault = isDefault,
            IsVerified = false,
            VerificationStatus = PayoutMethodVerificationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PayoutMethods.Add(payoutMethod);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added bank transfer payout method {PayoutMethodId} for store {StoreId}", payoutMethod.Id, storeId);

        result.Success = true;
        result.PayoutMethod = payoutMethod;
        return result;
    }

    /// <inheritdoc />
    public async Task<PayoutSettingsResult> UpdateBankTransferPayoutMethodAsync(int payoutMethodId, int storeId, BankTransferPayoutData data)
    {
        var result = new PayoutSettingsResult();

        // Validate required fields
        var validationErrors = ValidateBankTransferData(data);
        if (validationErrors.Count > 0)
        {
            result.Errors.AddRange(validationErrors);
            return result;
        }

        // Get the existing payout method
        var payoutMethod = await _context.PayoutMethods
            .FirstOrDefaultAsync(p => p.Id == payoutMethodId && p.StoreId == storeId);

        if (payoutMethod == null)
        {
            result.Errors.Add("Payout method not found.");
            return result;
        }

        // If this is set as default, remove default from other methods
        if (data.IsDefault && !payoutMethod.IsDefault)
        {
            await ClearDefaultPayoutMethodAsync(storeId);
        }

        // Update the payout method
        payoutMethod.DisplayName = data.DisplayName.Trim();
        payoutMethod.BankName = data.BankName.Trim();
        payoutMethod.BankAccountHolderName = data.BankAccountHolderName.Trim();
        payoutMethod.BankAccountNumberEncrypted = _encryptionService.Encrypt(data.BankAccountNumber.Trim());
        payoutMethod.BankAccountNumberLast4 = GetLast4Digits(data.BankAccountNumber.Trim());
        payoutMethod.BankRoutingNumber = data.BankRoutingNumber?.Trim();
        payoutMethod.Currency = string.IsNullOrWhiteSpace(data.Currency) ? null : data.Currency.Trim().ToUpperInvariant();
        payoutMethod.CountryCode = string.IsNullOrWhiteSpace(data.CountryCode) ? null : data.CountryCode.Trim().ToUpperInvariant();
        payoutMethod.IsDefault = data.IsDefault;
        payoutMethod.UpdatedAt = DateTime.UtcNow;

        // If bank details changed, reset verification status
        payoutMethod.IsVerified = false;
        payoutMethod.VerificationStatus = PayoutMethodVerificationStatus.Pending;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated bank transfer payout method {PayoutMethodId} for store {StoreId}", payoutMethodId, storeId);

        result.Success = true;
        result.PayoutMethod = payoutMethod;
        return result;
    }

    /// <inheritdoc />
    public async Task<PayoutSettingsResult> DeletePayoutMethodAsync(int payoutMethodId, int storeId)
    {
        var result = new PayoutSettingsResult();

        var payoutMethod = await _context.PayoutMethods
            .FirstOrDefaultAsync(p => p.Id == payoutMethodId && p.StoreId == storeId);

        if (payoutMethod == null)
        {
            result.Errors.Add("Payout method not found.");
            return result;
        }

        var wasDefault = payoutMethod.IsDefault;

        _context.PayoutMethods.Remove(payoutMethod);
        await _context.SaveChangesAsync();

        // If deleted method was default, set the first remaining method as default
        if (wasDefault)
        {
            var firstRemaining = await _context.PayoutMethods
                .Where(p => p.StoreId == storeId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (firstRemaining != null)
            {
                firstRemaining.IsDefault = true;
                await _context.SaveChangesAsync();
            }
        }

        _logger.LogInformation("Deleted payout method {PayoutMethodId} for store {StoreId}", payoutMethodId, storeId);

        result.Success = true;
        return result;
    }

    /// <inheritdoc />
    public async Task<PayoutSettingsResult> SetDefaultPayoutMethodAsync(int payoutMethodId, int storeId)
    {
        var result = new PayoutSettingsResult();

        var payoutMethod = await _context.PayoutMethods
            .FirstOrDefaultAsync(p => p.Id == payoutMethodId && p.StoreId == storeId);

        if (payoutMethod == null)
        {
            result.Errors.Add("Payout method not found.");
            return result;
        }

        if (payoutMethod.IsDefault)
        {
            // Already default
            result.Success = true;
            result.PayoutMethod = payoutMethod;
            return result;
        }

        // Clear default from other methods
        await ClearDefaultPayoutMethodAsync(storeId);

        // Set this method as default
        payoutMethod.IsDefault = true;
        payoutMethod.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Set payout method {PayoutMethodId} as default for store {StoreId}", payoutMethodId, storeId);

        result.Success = true;
        result.PayoutMethod = payoutMethod;
        return result;
    }

    /// <inheritdoc />
    public async Task<PayoutSettingsSummary> GetPayoutSettingsSummaryAsync(int storeId)
    {
        var payoutMethods = await GetPayoutMethodsAsync(storeId);
        var defaultMethod = payoutMethods.FirstOrDefault(p => p.IsDefault);

        var summary = new PayoutSettingsSummary
        {
            HasPayoutMethods = payoutMethods.Count > 0,
            HasDefaultMethod = defaultMethod != null,
            HasVerifiedMethod = payoutMethods.Any(p => p.IsVerified),
            DefaultPayoutMethod = defaultMethod,
            AllPayoutMethods = payoutMethods
        };

        // Determine configuration issues
        if (!summary.HasPayoutMethods)
        {
            summary.ConfigurationIssues.Add("No payout method configured. Please add a bank account to receive payouts.");
        }
        else if (!summary.HasDefaultMethod)
        {
            summary.ConfigurationIssues.Add("No default payout method set. Please select a default method for payouts.");
        }

        // Check if any method has verification issues
        var failedVerifications = payoutMethods.Where(p => p.VerificationStatus == PayoutMethodVerificationStatus.Failed).ToList();
        if (failedVerifications.Count > 0)
        {
            summary.ConfigurationIssues.Add("One or more payout methods failed verification. Please review and update your bank details.");
        }

        summary.IsPayoutConfigurationComplete = summary.HasPayoutMethods && 
                                                  summary.HasDefaultMethod && 
                                                  summary.ConfigurationIssues.Count == 0;

        return summary;
    }

    /// <inheritdoc />
    public async Task<bool> IsPayoutConfigurationCompleteAsync(int storeId)
    {
        var summary = await GetPayoutSettingsSummaryAsync(storeId);
        return summary.IsPayoutConfigurationComplete;
    }

    private async Task ClearDefaultPayoutMethodAsync(int storeId)
    {
        var currentDefaults = await _context.PayoutMethods
            .Where(p => p.StoreId == storeId && p.IsDefault)
            .ToListAsync();

        foreach (var method in currentDefaults)
        {
            method.IsDefault = false;
            method.UpdatedAt = DateTime.UtcNow;
        }

        if (currentDefaults.Count > 0)
        {
            await _context.SaveChangesAsync();
        }
    }

    private static List<string> ValidateBankTransferData(BankTransferPayoutData data)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(data.DisplayName))
        {
            errors.Add("Display name is required.");
        }
        else if (data.DisplayName.Length > 100)
        {
            errors.Add("Display name must be 100 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(data.BankName))
        {
            errors.Add("Bank name is required.");
        }
        else if (data.BankName.Length > 100)
        {
            errors.Add("Bank name must be 100 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(data.BankAccountHolderName))
        {
            errors.Add("Bank account holder name is required.");
        }
        else if (data.BankAccountHolderName.Length > 100)
        {
            errors.Add("Bank account holder name must be 100 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(data.BankAccountNumber))
        {
            errors.Add("Bank account number is required.");
        }
        else if (data.BankAccountNumber.Length > 100)
        {
            errors.Add("Bank account number must be 100 characters or less.");
        }

        if (!string.IsNullOrWhiteSpace(data.BankRoutingNumber) && data.BankRoutingNumber.Length > 50)
        {
            errors.Add("Bank routing number must be 50 characters or less.");
        }

        if (!string.IsNullOrWhiteSpace(data.Currency))
        {
            var currency = data.Currency.Trim().ToUpperInvariant();
            if (currency.Length != 3 || !IsValidCurrencyCode(currency))
            {
                errors.Add("Currency must be a valid 3-letter ISO 4217 code (e.g., USD, EUR).");
            }
        }

        if (!string.IsNullOrWhiteSpace(data.CountryCode))
        {
            var country = data.CountryCode.Trim().ToUpperInvariant();
            if (country.Length != 2 || !IsValidCountryCode(country))
            {
                errors.Add("Country code must be a valid 2-letter ISO 3166-1 alpha-2 code (e.g., US, GB).");
            }
        }

        return errors;
    }

    // Common ISO 4217 currency codes
    private static readonly HashSet<string> ValidCurrencyCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD", "EUR", "GBP", "CAD", "AUD", "JPY", "CHF", "CNY", "INR", "PLN",
        "SEK", "NOK", "DKK", "NZD", "SGD", "HKD", "KRW", "MXN", "BRL", "ZAR"
    };

    // Common ISO 3166-1 alpha-2 country codes
    private static readonly HashSet<string> ValidCountryCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "US", "GB", "DE", "FR", "CA", "AU", "JP", "CH", "CN", "IN", "PL",
        "SE", "NO", "DK", "NZ", "SG", "HK", "KR", "MX", "BR", "ZA", "NL",
        "BE", "IT", "ES", "PT", "AT", "IE", "FI", "CZ", "HU", "RO", "BG"
    };

    private static bool IsValidCurrencyCode(string code)
    {
        return ValidCurrencyCodes.Contains(code);
    }

    private static bool IsValidCountryCode(string code)
    {
        return ValidCountryCodes.Contains(code);
    }

    private static string GetLast4Digits(string accountNumber)
    {
        if (string.IsNullOrEmpty(accountNumber))
        {
            return string.Empty;
        }

        // Remove non-alphanumeric characters for last 4 extraction
        var cleanedNumber = new string(accountNumber.Where(char.IsLetterOrDigit).ToArray());
        
        if (cleanedNumber.Length <= 4)
        {
            return cleanedNumber;
        }

        return cleanedNumber[^4..];
    }
}
