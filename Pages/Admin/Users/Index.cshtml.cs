using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Users;

/// <summary>
/// Page model for admin user management list.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class IndexModel : PageModel
{
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IUserManagementService userManagementService,
        ILogger<IndexModel> logger)
    {
        _userManagementService = userManagementService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the filter criteria.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public UserManagementFilter Filter { get; set; } = new();

    /// <summary>
    /// Gets or sets the paginated user list.
    /// </summary>
    public PaginatedList<UserListItem>? Users { get; set; }

    /// <summary>
    /// Handles GET request to display the user list.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // Get users based on filter criteria
            Users = await _userManagementService.GetUsersAsync(Filter);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user management list");
            TempData["ErrorMessage"] = "An error occurred while loading the user list.";
            return Page();
        }
    }
}
