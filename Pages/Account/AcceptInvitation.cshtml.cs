using System.Security.Claims;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Account;

public class AcceptInvitationModel : PageModel
{
    private readonly IInternalUserManagementService _internalUserService;
    private readonly IFeatureFlagService _featureFlagService;

    public AcceptInvitationModel(
        IInternalUserManagementService internalUserService,
        IFeatureFlagService featureFlagService)
    {
        _internalUserService = internalUserService;
        _featureFlagService = featureFlagService;
    }

    public bool InvitationValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string Token { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string InvitedByName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string RoleDescription { get; set; } = string.Empty;
    public string InvitationEmail { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }

    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;
    public string? CurrentUserEmail => User.FindFirst(ClaimTypes.Email)?.Value;
    public bool EmailMatches => string.Equals(CurrentUserEmail, InvitationEmail, StringComparison.OrdinalIgnoreCase);

    public async Task<IActionResult> OnGetAsync(string token)
    {
        // Check if feature is enabled
        if (!_featureFlagService.IsSellerUserManagementEnabled)
        {
            InvitationValid = false;
            ErrorMessage = "This feature is not currently available.";
            return Page();
        }

        Token = token;

        var invitation = await _internalUserService.GetInvitationByTokenAsync(token);

        if (invitation == null)
        {
            InvitationValid = false;
            ErrorMessage = "This invitation link is invalid or has expired.";
            return Page();
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            InvitationValid = false;
            ErrorMessage = invitation.Status switch
            {
                InvitationStatus.Accepted => "This invitation has already been accepted.",
                InvitationStatus.Expired => "This invitation has expired.",
                InvitationStatus.Revoked => "This invitation has been revoked.",
                _ => "This invitation is no longer valid."
            };
            return Page();
        }

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            InvitationValid = false;
            ErrorMessage = "This invitation has expired.";
            return Page();
        }

        InvitationValid = true;
        StoreName = invitation.Store?.StoreName ?? "Unknown Store";
        InvitedByName = invitation.InvitedByUser != null
            ? $"{invitation.InvitedByUser.FirstName} {invitation.InvitedByUser.LastName}".Trim()
            : "Store Owner";
        RoleName = StoreRoleNames.GetDisplayName(invitation.Role);
        RoleDescription = StoreRoleNames.GetDescription(invitation.Role);
        InvitationEmail = invitation.Email;
        ExpiresAt = invitation.ExpiresAt;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string token)
    {
        // Check if feature is enabled
        if (!_featureFlagService.IsSellerUserManagementEnabled)
        {
            InvitationValid = false;
            ErrorMessage = "This feature is not currently available.";
            return Page();
        }

        Token = token;

        if (!IsAuthenticated)
        {
            return RedirectToPage("/Account/Login", new { returnUrl = $"/Account/AcceptInvitation/{token}" });
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            InvitationValid = false;
            ErrorMessage = "Unable to identify your account.";
            return Page();
        }

        var result = await _internalUserService.AcceptInvitationAsync(token, userId.Value);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "You have successfully joined the store team!";
            return RedirectToPage("/Seller/StoreSettings");
        }

        // Re-fetch invitation for display
        var invitation = await _internalUserService.GetInvitationByTokenAsync(token);
        if (invitation != null)
        {
            InvitationValid = true;
            StoreName = invitation.Store?.StoreName ?? "Unknown Store";
            InvitedByName = invitation.InvitedByUser != null
                ? $"{invitation.InvitedByUser.FirstName} {invitation.InvitedByUser.LastName}".Trim()
                : "Store Owner";
            RoleName = StoreRoleNames.GetDisplayName(invitation.Role);
            RoleDescription = StoreRoleNames.GetDescription(invitation.Role);
            InvitationEmail = invitation.Email;
            ExpiresAt = invitation.ExpiresAt;
        }
        else
        {
            InvitationValid = false;
        }

        ErrorMessage = result.ErrorMessage ?? "Failed to accept the invitation.";
        return Page();
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}
