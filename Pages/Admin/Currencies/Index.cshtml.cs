using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Currencies;

/// <summary>
/// Page model for listing and managing currencies.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly ICurrencyService _currencyService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ICurrencyService currencyService,
        ILogger<IndexModel> logger)
    {
        _currencyService = currencyService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of all currencies.
    /// </summary>
    public List<Currency> Currencies { get; set; } = new();

    /// <summary>
    /// Gets or sets the currency configuration.
    /// </summary>
    public CurrencyConfig Config { get; set; } = null!;

    /// <summary>
    /// Gets or sets the filter for enabled/disabled currencies.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string Filter { get; set; } = "all";

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // Initialize default currencies if none exist
            await _currencyService.InitializeDefaultCurrenciesAsync();

            // Get all currencies or only enabled ones based on filter
            Currencies = await _currencyService.GetAllCurrenciesAsync(enabledOnly: Filter == "enabled");
            
            // Get currency configuration
            Config = await _currencyService.GetCurrencyConfigAsync();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading currencies");
            ErrorMessage = "An error occurred while loading currencies.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostEnableAsync(int id)
    {
        try
        {
            var success = await _currencyService.EnableCurrencyAsync(id);
            if (success)
            {
                SuccessMessage = "Currency enabled successfully.";
            }
            else
            {
                ErrorMessage = "Currency not found.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling currency {Id}", id);
            ErrorMessage = $"Error enabling currency: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDisableAsync(int id)
    {
        try
        {
            var success = await _currencyService.DisableCurrencyAsync(id);
            if (success)
            {
                SuccessMessage = "Currency disabled successfully. It will not be available for new listings or transactions.";
            }
            else
            {
                ErrorMessage = "Currency not found.";
            }
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling currency {Id}", id);
            ErrorMessage = $"Error disabling currency: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var currency = await _currencyService.GetCurrencyByIdAsync(id);
            if (currency == null)
            {
                ErrorMessage = "Currency not found.";
                return RedirectToPage();
            }

            var success = await _currencyService.DeleteCurrencyAsync(id);
            if (success)
            {
                SuccessMessage = $"Currency {currency.Code} deleted successfully.";
            }
            else
            {
                ErrorMessage = "Failed to delete currency.";
            }
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting currency {Id}", id);
            ErrorMessage = $"Error deleting currency: {ex.Message}";
        }

        return RedirectToPage();
    }
}
