using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Pages.Admin.Currencies;

/// <summary>
/// Page model for editing a currency.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class EditModel : PageModel
{
    private readonly ICurrencyService _currencyService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        ICurrencyService currencyService,
        ILogger<EditModel> logger)
    {
        _currencyService = currencyService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public Currency Currency { get; set; } = null!;

    public bool IsBaseCurrency { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        [Display(Name = "Symbol")]
        public string Symbol { get; set; } = string.Empty;

        [Required]
        [Range(0, 4)]
        [Display(Name = "Decimal Places")]
        public int DecimalPlaces { get; set; } = 2;

        [Required]
        [Range(0.00000001, 999999999)]
        [Display(Name = "Exchange Rate")]
        public decimal ExchangeRate { get; set; } = 1.0m;

        [MaxLength(100)]
        [Display(Name = "Exchange Rate Source")]
        public string? ExchangeRateSource { get; set; }

        [Required]
        [Range(0, 1000)]
        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Enabled")]
        public bool IsEnabled { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Currency = await _currencyService.GetCurrencyByIdAsync(id.Value);
        if (Currency == null)
        {
            return NotFound();
        }

        var config = await _currencyService.GetCurrencyConfigAsync();
        IsBaseCurrency = Currency.Code == config.BaseCurrencyCode;

        Input = new InputModel
        {
            Id = Currency.Id,
            Name = Currency.Name,
            Symbol = Currency.Symbol,
            DecimalPlaces = Currency.DecimalPlaces,
            ExchangeRate = Currency.ExchangeRate,
            ExchangeRateSource = Currency.ExchangeRateSource,
            DisplayOrder = Currency.DisplayOrder,
            IsEnabled = Currency.IsEnabled
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Currency = await _currencyService.GetCurrencyByIdAsync(Input.Id);
            if (Currency == null)
            {
                return NotFound();
            }
            return Page();
        }

        try
        {
            var currency = await _currencyService.GetCurrencyByIdAsync(Input.Id);
            if (currency == null)
            {
                ErrorMessage = "Currency not found.";
                return RedirectToPage("Index");
            }

            var config = await _currencyService.GetCurrencyConfigAsync();
            IsBaseCurrency = currency.Code == config.BaseCurrencyCode;

            currency.Name = Input.Name;
            currency.Symbol = Input.Symbol;
            currency.DecimalPlaces = Input.DecimalPlaces;
            currency.ExchangeRate = Input.ExchangeRate;
            currency.ExchangeRateSource = Input.ExchangeRateSource;
            currency.ExchangeRateLastUpdated = DateTime.UtcNow;
            currency.DisplayOrder = Input.DisplayOrder;
            currency.IsEnabled = Input.IsEnabled;

            await _currencyService.UpdateCurrencyAsync(currency);

            SuccessMessage = $"Currency {currency.Code} updated successfully.";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating currency {Id}", Input.Id);
            ErrorMessage = $"Error updating currency: {ex.Message}";
            
            Currency = await _currencyService.GetCurrencyByIdAsync(Input.Id);
            return Page();
        }
    }
}
