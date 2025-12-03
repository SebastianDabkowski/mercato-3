using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Pages.Admin.Currencies;

/// <summary>
/// Page model for creating a new currency.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class CreateModel : PageModel
{
    private readonly ICurrencyService _currencyService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        ICurrencyService currencyService,
        ILogger<CreateModel> logger)
    {
        _currencyService = currencyService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [MaxLength(3)]
        [Display(Name = "Currency Code")]
        [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Currency code must be 3 uppercase letters (ISO 4217 format)")]
        public string Code { get; set; } = string.Empty;

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

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var currency = new Currency
            {
                Code = Input.Code.ToUpper(),
                Name = Input.Name,
                Symbol = Input.Symbol,
                DecimalPlaces = Input.DecimalPlaces,
                ExchangeRate = Input.ExchangeRate,
                ExchangeRateSource = Input.ExchangeRateSource ?? "Manual",
                ExchangeRateLastUpdated = DateTime.UtcNow,
                DisplayOrder = Input.DisplayOrder,
                IsEnabled = Input.IsEnabled
            };

            await _currencyService.CreateCurrencyAsync(currency);

            SuccessMessage = $"Currency {currency.Code} created successfully.";
            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating currency");
            ErrorMessage = $"Error creating currency: {ex.Message}";
            return Page();
        }
    }
}
