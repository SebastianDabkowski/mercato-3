using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Pages.Admin.Users;

/// <summary>
/// Page model for blocking a user account.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class BlockModel : PageModel
{
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<BlockModel> _logger;

    public BlockModel(
        IUserManagementService userManagementService,
        ILogger<BlockModel> logger)
    {
        _userManagementService = userManagementService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the user to be blocked.
    /// </summary>
    public new User? User { get; set; }

    /// <summary>
    /// Gets or sets the blocking reason.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Please select a reason for blocking this account.")]
    public BlockReason Reason { get; set; }

    /// <summary>
    /// Gets or sets additional notes about the blocking.
    /// </summary>
    [BindProperty]
    [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Handles GET request to display the block confirmation page.
    /// </summary>
    /// <param name="id">The user ID to block.</param>
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

            // Check if user is already blocked
            if (User.Status == AccountStatus.Blocked)
            {
                TempData["ErrorMessage"] = "This user is already blocked.";
                return RedirectToPage("/Admin/Users/Details", new { id });
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading block page for user {UserId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the page.";
            return RedirectToPage("/Admin/Users/Index");
        }
    }

    /// <summary>
    /// Handles POST request to block the user account.
    /// </summary>
    /// <param name="id">The user ID to block.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                User = await _userManagementService.GetUserDetailsAsync(id);
                return Page();
            }

            // Get the current admin user ID
            var adminUserIdClaim = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (adminUserIdClaim == null || !int.TryParse(adminUserIdClaim.Value, out var adminUserId))
            {
                TempData["ErrorMessage"] = "Unable to identify the current admin user.";
                return RedirectToPage("/Admin/Users/Details", new { id });
            }

            // Block the user
            var success = await _userManagementService.BlockUserAsync(id, adminUserId, Reason, Notes);

            if (success)
            {
                TempData["SuccessMessage"] = "User account has been blocked successfully.";
                _logger.LogInformation("Admin {AdminUserId} blocked user {UserId} for reason {Reason}", 
                    adminUserId, id, Reason);
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to block the user account. The user may not exist or is already blocked.";
            }

            return RedirectToPage("/Admin/Users/Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blocking user {UserId}", id);
            TempData["ErrorMessage"] = "An error occurred while blocking the user account.";
            return RedirectToPage("/Admin/Users/Details", new { id });
        }
    }
}
