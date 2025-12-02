using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Users;

/// <summary>
/// Page model for displaying detailed user information.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class DetailsModel : PageModel
{
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IUserManagementService userManagementService,
        ILogger<DetailsModel> logger)
    {
        _userManagementService = userManagementService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the user details.
    /// </summary>
    public new User? User { get; set; }

    /// <summary>
    /// Gets or sets the user's role.
    /// </summary>
    public string UserRole { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the login history for the user.
    /// </summary>
    public List<LoginEvent>? LoginHistory { get; set; }

    /// <summary>
    /// Handles GET request to display user details.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            // Get user details
            User = await _userManagementService.GetUserDetailsAsync(id);

            if (User == null)
            {
                return NotFound();
            }

            // Get user role
            UserRole = await _userManagementService.GetUserRoleAsync(id);

            // Get login history
            LoginHistory = await _userManagementService.GetLoginHistoryAsync(id, 20);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user details for user {UserId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the user details.";
            return RedirectToPage("/Admin/Users/Index");
        }
    }
}
