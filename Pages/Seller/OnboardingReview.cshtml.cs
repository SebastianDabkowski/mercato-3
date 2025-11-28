using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Seller;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class OnboardingReviewModel : PageModel
{
    private readonly ISellerOnboardingService _onboardingService;

    public OnboardingReviewModel(ISellerOnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    public SellerOnboardingDraft? Draft { get; set; }
    
    public string MaskedAccountNumber { get; set; } = string.Empty;

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

        // Load draft data
        Draft = await _onboardingService.GetOrCreateDraftAsync(userId.Value);
        
        // Ensure all steps are completed
        if (Draft.LastCompletedStep < 3)
        {
            // Redirect to the appropriate step
            return Draft.LastCompletedStep switch
            {
                0 => RedirectToPage("OnboardingStep1"),
                1 => RedirectToPage("OnboardingStep2"),
                2 => RedirectToPage("OnboardingStep3"),
                _ => RedirectToPage("OnboardingStep1")
            };
        }

        // Mask account number for display
        if (!string.IsNullOrEmpty(Draft.BankAccountNumber))
        {
            MaskedAccountNumber = MaskAccountNumber(Draft.BankAccountNumber);
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

        // Check if user already has a store
        if (await _onboardingService.HasExistingStoreAsync(userId.Value))
        {
            return RedirectToPage("OnboardingComplete");
        }

        var result = await _onboardingService.CompleteOnboardingAsync(userId.Value);

        if (!result.Success)
        {
            // Reload draft for display
            Draft = await _onboardingService.GetOrCreateDraftAsync(userId.Value);
            if (!string.IsNullOrEmpty(Draft.BankAccountNumber))
            {
                MaskedAccountNumber = MaskAccountNumber(Draft.BankAccountNumber);
            }
            
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            return Page();
        }

        return RedirectToPage("OnboardingComplete");
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

    private static string MaskAccountNumber(string accountNumber)
    {
        if (accountNumber.Length <= 4)
        {
            return new string('*', accountNumber.Length);
        }
        
        return new string('*', accountNumber.Length - 4) + accountNumber[^4..];
    }
}
