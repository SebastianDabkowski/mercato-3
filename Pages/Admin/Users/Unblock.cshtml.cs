using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Pages.Admin.Users;

/// <summary>
/// Page model for unblocking a user account.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class UnblockModel : PageModel
{
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<UnblockModel> _logger;

    public UnblockModel(
        IUserManagementService userManagementService,
        ILogger<UnblockModel> logger)
    {
        _userManagementService = userManagementService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the user to be unblocked.
    /// </summary>
    public new User? User { get; set; }

    /// <summary>
    /// Gets or sets the admin user who blocked this account.
    /// </summary>
    public User? BlockedByAdmin { get; set; }

    /// <summary>
    /// Gets or sets additional notes about the unblocking.
    /// </summary>
    [BindProperty]
    [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Handles GET request to display the unblock confirmation page.
    /// </summary>
    /// <param name="id">The user ID to unblock.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            User = await _userManagementService.GetUserDetailsAsync(id);

            if (User == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToPage("/Admin/Users/Index");
            }

            // Check if user is actually blocked
            if (User.Status != AccountStatus.Blocked)
            {
                TempData["ErrorMessage"] = "This user is not currently blocked.";
                return RedirectToPage("/Admin/Users/Details", new { id });
            }

            // Get blocked by admin info
            if (User.BlockedByUserId.HasValue)
            {
                BlockedByAdmin = await _userManagementService.GetUserDetailsAsync(User.BlockedByUserId.Value);
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading unblock page for user {UserId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the page.";
            return RedirectToPage("/Admin/Users/Index");
        }
    }

    /// <summary>
    /// Handles POST request to unblock the user account.
    /// </summary>
    /// <param name="id">The user ID to unblock.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        try
        {
            // Get the current admin user ID
            var adminUserIdClaim = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (adminUserIdClaim == null || !int.TryParse(adminUserIdClaim.Value, out var adminUserId))
            {
                TempData["ErrorMessage"] = "Unable to identify the current admin user.";
                return RedirectToPage("/Admin/Users/Details", new { id });
            }

            // Unblock the user
            var success = await _userManagementService.UnblockUserAsync(id, adminUserId, Notes);

            if (success)
            {
                TempData["SuccessMessage"] = "User account has been unblocked successfully.";
                _logger.LogInformation("Admin {AdminUserId} unblocked user {UserId}", adminUserId, id);
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to unblock the user account. The user may not exist or is not currently blocked.";
            }

            return RedirectToPage("/Admin/Users/Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unblocking user {UserId}", id);
            TempData["ErrorMessage"] = "An error occurred while unblocking the user account.";
            return RedirectToPage("/Admin/Users/Details", new { id });
        }
    }
}
