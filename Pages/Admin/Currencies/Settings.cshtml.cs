using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Pages.Admin.Currencies;

/// <summary>
/// Page model for managing currency configuration settings.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class SettingsModel : PageModel
{
    private readonly ICurrencyService _currencyService;
    private readonly ILogger<SettingsModel> _logger;

    public SettingsModel(
        ICurrencyService currencyService,
        ILogger<SettingsModel> logger)
    {
        _currencyService = currencyService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public CurrencyConfig Config { get; set; } = null!;

    public List<Currency> AvailableCurrencies { get; set; } = new();

    public string CurrentBaseCurrency { get; set; } = string.Empty;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [MaxLength(3)]
        [Display(Name = "Base Currency Code")]
        public string BaseCurrencyCode { get; set; } = "USD";

        [Display(Name = "Auto Update Exchange Rates")]
        public bool AutoUpdateExchangeRates { get; set; }

        [Range(1, 168)]
        [Display(Name = "Update Frequency (hours)")]
        public int UpdateFrequencyHours { get; set; } = 24;

        [MaxLength(1000)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Confirm Change")]
        public bool ConfirmChange { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            Config = await _currencyService.GetCurrencyConfigAsync();
            CurrentBaseCurrency = Config.BaseCurrencyCode;

            // Get all enabled currencies for the dropdown
            AvailableCurrencies = await _currencyService.GetAllCurrenciesAsync(enabledOnly: true);

            Input = new InputModel
            {
                BaseCurrencyCode = Config.BaseCurrencyCode,
                AutoUpdateExchangeRates = Config.AutoUpdateExchangeRates,
                UpdateFrequencyHours = Config.UpdateFrequencyHours,
                Notes = Config.Notes
            };

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading currency settings");
            ErrorMessage = "An error occurred while loading currency settings.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Load current configuration
        Config = await _currencyService.GetCurrencyConfigAsync();
        CurrentBaseCurrency = Config.BaseCurrencyCode;
        AvailableCurrencies = await _currencyService.GetAllCurrenciesAsync(enabledOnly: true);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            // Check if base currency is being changed
            bool isChangingBaseCurrency = Input.BaseCurrencyCode != Config.BaseCurrencyCode;

            if (isChangingBaseCurrency && !Input.ConfirmChange)
            {
                ModelState.AddModelError("Input.ConfirmChange", 
                    "You must confirm this change. Changing the base currency is a major operation that affects all currency calculations.");
                return Page();
            }

            // Get current user ID from claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                ErrorMessage = "Unable to identify current user.";
                return Page();
            }

            var updatedConfig = new CurrencyConfig
            {
                BaseCurrencyCode = Input.BaseCurrencyCode,
                AutoUpdateExchangeRates = Input.AutoUpdateExchangeRates,
                UpdateFrequencyHours = Input.UpdateFrequencyHours,
                Notes = Input.Notes
            };

            await _currencyService.UpdateCurrencyConfigAsync(updatedConfig, userId);

            if (isChangingBaseCurrency)
            {
                SuccessMessage = $"Base currency changed from {Config.BaseCurrencyCode} to {Input.BaseCurrencyCode} successfully. Please review all currency exchange rates.";
            }
            else
            {
                SuccessMessage = "Currency settings updated successfully.";
            }

            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating currency settings");
            ErrorMessage = $"Error updating currency settings: {ex.Message}";
            return Page();
        }
    }
}
