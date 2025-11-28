using System.ComponentModel.DataAnnotations;
using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Seller;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class PayoutSettingsModel : PageModel
{
    private readonly IPayoutSettingsService _payoutSettingsService;
    private readonly IStoreProfileService _storeProfileService;

    public PayoutSettingsModel(
        IPayoutSettingsService payoutSettingsService,
        IStoreProfileService storeProfileService)
    {
        _payoutSettingsService = payoutSettingsService;
        _storeProfileService = storeProfileService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public Store? Store { get; set; }

    public PayoutSettingsSummary? PayoutSummary { get; set; }

    public List<PayoutMethod> PayoutMethods { get; set; } = new();

    public PayoutMethod? EditingMethod { get; set; }

    public bool IsEditing { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Display name is required.")]
        [MaxLength(100, ErrorMessage = "Display name must be 100 characters or less.")]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bank name is required.")]
        [MaxLength(100, ErrorMessage = "Bank name must be 100 characters or less.")]
        [Display(Name = "Bank Name")]
        public string BankName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account holder name is required.")]
        [MaxLength(100, ErrorMessage = "Account holder name must be 100 characters or less.")]
        [Display(Name = "Account Holder Name")]
        public string BankAccountHolderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account number is required.")]
        [MaxLength(100, ErrorMessage = "Account number must be 100 characters or less.")]
        [Display(Name = "Account Number / IBAN")]
        public string BankAccountNumber { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "Routing number must be 50 characters or less.")]
        [Display(Name = "Routing Number / SWIFT / BIC")]
        public string? BankRoutingNumber { get; set; }

        [MaxLength(3, ErrorMessage = "Currency must be a 3-letter code.")]
        [Display(Name = "Currency (e.g., USD, EUR)")]
        public string? Currency { get; set; }

        [MaxLength(2, ErrorMessage = "Country code must be a 2-letter code.")]
        [Display(Name = "Country Code (e.g., US, GB)")]
        public string? CountryCode { get; set; }

        [Display(Name = "Set as Default Payout Method")]
        public bool IsDefault { get; set; }

        public int? EditingMethodId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int? edit = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        Store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (Store == null)
        {
            return RedirectToPage("OnboardingStep1");
        }

        PayoutMethods = await _payoutSettingsService.GetPayoutMethodsAsync(Store.Id);
        PayoutSummary = await _payoutSettingsService.GetPayoutSettingsSummaryAsync(Store.Id);

        // If editing an existing method
        if (edit.HasValue)
        {
            EditingMethod = await _payoutSettingsService.GetPayoutMethodAsync(edit.Value, Store.Id);
            if (EditingMethod != null)
            {
                IsEditing = true;
                Input.EditingMethodId = EditingMethod.Id;
                Input.DisplayName = EditingMethod.DisplayName;
                Input.BankName = EditingMethod.BankName ?? string.Empty;
                Input.BankAccountHolderName = EditingMethod.BankAccountHolderName ?? string.Empty;
                // Don't expose the full account number for security, require re-entry
                Input.BankAccountNumber = string.Empty;
                Input.BankRoutingNumber = EditingMethod.BankRoutingNumber;
                Input.Currency = EditingMethod.Currency;
                Input.CountryCode = EditingMethod.CountryCode;
                Input.IsDefault = EditingMethod.IsDefault;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        Store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (Store == null)
        {
            return RedirectToPage("OnboardingStep1");
        }

        PayoutMethods = await _payoutSettingsService.GetPayoutMethodsAsync(Store.Id);
        PayoutSummary = await _payoutSettingsService.GetPayoutSettingsSummaryAsync(Store.Id);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var data = new BankTransferPayoutData
        {
            DisplayName = Input.DisplayName,
            BankName = Input.BankName,
            BankAccountHolderName = Input.BankAccountHolderName,
            BankAccountNumber = Input.BankAccountNumber,
            BankRoutingNumber = Input.BankRoutingNumber,
            Currency = Input.Currency,
            CountryCode = Input.CountryCode,
            IsDefault = Input.IsDefault
        };

        PayoutSettingsResult result;

        if (Input.EditingMethodId.HasValue)
        {
            // Update existing method
            result = await _payoutSettingsService.UpdateBankTransferPayoutMethodAsync(
                Input.EditingMethodId.Value, Store.Id, data);
            
            if (result.Success)
            {
                SuccessMessage = "Payout method updated successfully.";
            }
        }
        else
        {
            // Add new method
            result = await _payoutSettingsService.AddBankTransferPayoutMethodAsync(Store.Id, data);
            
            if (result.Success)
            {
                SuccessMessage = "Payout method added successfully.";
            }
        }

        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            return Page();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSetDefaultAsync(int methodId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        Store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (Store == null)
        {
            return RedirectToPage("OnboardingStep1");
        }

        var result = await _payoutSettingsService.SetDefaultPayoutMethodAsync(methodId, Store.Id);

        if (result.Success)
        {
            SuccessMessage = "Default payout method updated successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int methodId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        Store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (Store == null)
        {
            return RedirectToPage("OnboardingStep1");
        }

        var result = await _payoutSettingsService.DeletePayoutMethodAsync(methodId, Store.Id);

        if (result.Success)
        {
            SuccessMessage = "Payout method deleted successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
        }

        return RedirectToPage();
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}
