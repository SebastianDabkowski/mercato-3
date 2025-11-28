using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Seller;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class OnboardingCompleteModel : PageModel
{
    private readonly ISellerOnboardingService _onboardingService;
    private readonly IPayoutSettingsService _payoutSettingsService;

    public OnboardingCompleteModel(
        ISellerOnboardingService onboardingService,
        IPayoutSettingsService payoutSettingsService)
    {
        _onboardingService = onboardingService;
        _payoutSettingsService = payoutSettingsService;
    }

    public Store? Store { get; set; }

    public PayoutSettingsSummary? PayoutSummary { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        // Check if user has a store
        Store = await _onboardingService.GetStoreAsync(userId.Value);
        
        // If no store exists, redirect to start onboarding
        if (Store == null)
        {
            return RedirectToPage("OnboardingStep1");
        }

        // Get payout settings summary
        PayoutSummary = await _payoutSettingsService.GetPayoutSettingsSummaryAsync(Store.Id);

        return Page();
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
