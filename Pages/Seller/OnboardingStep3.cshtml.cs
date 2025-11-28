using System.ComponentModel.DataAnnotations;
using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Seller;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class OnboardingStep3Model : PageModel
{
    private readonly ISellerOnboardingService _onboardingService;

    public OnboardingStep3Model(ISellerOnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
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
        [Display(Name = "Routing Number / SWIFT")]
        public string? BankRoutingNumber { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        // Check if user already has a store
        if (await _onboardingService.HasExistingStoreAsync(userId.Value))
        {
            return RedirectToPage("OnboardingComplete");
        }

        // Load existing draft data
        var draft = await _onboardingService.GetOrCreateDraftAsync(userId.Value);
        
        // Ensure Step 2 is completed
        if (draft.LastCompletedStep < 2)
        {
            return RedirectToPage("OnboardingStep2");
        }

        Input.BankName = draft.BankName ?? string.Empty;
        Input.BankAccountHolderName = draft.BankAccountHolderName ?? string.Empty;
        Input.BankAccountNumber = draft.BankAccountNumber ?? string.Empty;
        Input.BankRoutingNumber = draft.BankRoutingNumber;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        // Check if user already has a store
        if (await _onboardingService.HasExistingStoreAsync(userId.Value))
        {
            return RedirectToPage("OnboardingComplete");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var data = new PayoutData
        {
            BankName = Input.BankName,
            BankAccountHolderName = Input.BankAccountHolderName,
            BankAccountNumber = Input.BankAccountNumber,
            BankRoutingNumber = Input.BankRoutingNumber
        };

        var result = await _onboardingService.SavePayoutDataAsync(userId.Value, data);

        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            return Page();
        }

        return RedirectToPage("OnboardingReview");
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
