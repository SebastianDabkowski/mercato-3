using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MercatoApp.Pages.Account;

[Authorize]
public class PrivacySettingsModel : PageModel
{
    private readonly IConsentManagementService _consentService;
    private readonly ILogger<PrivacySettingsModel> _logger;

    public PrivacySettingsModel(
        IConsentManagementService consentService,
        ILogger<PrivacySettingsModel> logger)
    {
        _consentService = consentService;
        _logger = logger;
    }

    public Dictionary<ConsentType, ConsentStatus> Consents { get; set; } = new();
    
    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class ConsentStatus
    {
        public bool IsGranted { get; set; }
        public DateTime? ConsentedAt { get; set; }
        public string? Version { get; set; }
        public string? ConsentText { get; set; }
        public bool IsRequired { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return RedirectToPage("/Account/Login");
        }

        await LoadConsentsAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateConsentAsync(string consentType, bool granted)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return RedirectToPage("/Account/Login");
        }

        if (!Enum.TryParse<ConsentType>(consentType, out var type))
        {
            ErrorMessage = "Invalid consent type.";
            await LoadConsentsAsync(userId);
            return Page();
        }

        // Prevent changing required consents
        if (type == ConsentType.TermsOfService || type == ConsentType.PrivacyPolicy)
        {
            ErrorMessage = "Required consents cannot be changed. Please contact support if you wish to delete your account.";
            await LoadConsentsAsync(userId);
            return Page();
        }

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            if (granted)
            {
                await _consentService.GrantConsentAsync(
                    userId,
                    type,
                    version: "1.0",
                    consentText: GetConsentText(type),
                    ipAddress,
                    userAgent,
                    context: "privacy_settings");

                SuccessMessage = $"{GetConsentDisplayName(type)} consent granted successfully.";
            }
            else
            {
                await _consentService.WithdrawConsentAsync(
                    userId,
                    type,
                    ipAddress,
                    userAgent,
                    context: "privacy_settings");

                SuccessMessage = $"{GetConsentDisplayName(type)} consent withdrawn successfully.";
            }

            _logger.LogInformation(
                "User {UserId} {Action} consent for {ConsentType}",
                userId,
                granted ? "granted" : "withdrew",
                type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating consent for user {UserId}", userId);
            ErrorMessage = "An error occurred while updating your consent. Please try again.";
        }

        await LoadConsentsAsync(userId);
        return Page();
    }

    private async Task LoadConsentsAsync(int userId)
    {
        var currentConsents = await _consentService.GetCurrentConsentsAsync(userId);

        // Define all consent types users can manage
        var consentTypes = new[]
        {
            ConsentType.Newsletter,
            ConsentType.Marketing,
            ConsentType.Profiling,
            ConsentType.ThirdPartySharing,
            ConsentType.TermsOfService,
            ConsentType.PrivacyPolicy
        };

        foreach (var type in consentTypes)
        {
            var hasConsent = currentConsents.TryGetValue(type, out var consent);
            
            Consents[type] = new ConsentStatus
            {
                IsGranted = hasConsent && consent?.IsGranted == true,
                ConsentedAt = consent?.ConsentedAt,
                Version = consent?.ConsentVersion,
                ConsentText = consent?.ConsentText,
                IsRequired = type == ConsentType.TermsOfService || type == ConsentType.PrivacyPolicy
            };
        }
    }

    private string GetConsentDisplayName(ConsentType type)
    {
        return type switch
        {
            ConsentType.Newsletter => "Newsletter",
            ConsentType.Marketing => "Marketing Communications",
            ConsentType.Profiling => "Personalization and Profiling",
            ConsentType.ThirdPartySharing => "Third-Party Data Sharing",
            ConsentType.TermsOfService => "Terms of Service",
            ConsentType.PrivacyPolicy => "Privacy Policy",
            _ => type.ToString()
        };
    }

    private string GetConsentText(ConsentType type)
    {
        return type switch
        {
            ConsentType.Newsletter => "I agree to receive newsletters and general updates from Mercato.",
            ConsentType.Marketing => "I agree to receive marketing communications and promotional offers from Mercato.",
            ConsentType.Profiling => "I agree to allow Mercato to analyze my behavior and provide personalized recommendations.",
            ConsentType.ThirdPartySharing => "I agree to allow Mercato to share my data with trusted third-party partners.",
            _ => string.Empty
        };
    }
}
