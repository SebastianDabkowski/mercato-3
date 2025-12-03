using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MercatoApp.Pages.Admin.Users;

/// <summary>
/// Page model for displaying detailed user information.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class DetailsModel : PageModel
{
    private readonly IUserManagementService _userManagementService;
    private readonly IAdminAuditLogService _auditLogService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IUserManagementService userManagementService,
        IAdminAuditLogService auditLogService,
        ILogger<DetailsModel> logger)
    {
        _userManagementService = userManagementService;
        _auditLogService = auditLogService;
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
    /// Gets or sets the audit log entries for the user.
    /// </summary>
    public List<AdminAuditLog>? AuditLog { get; set; }

    /// <summary>
    /// Gets or sets the admin user who blocked this account (if blocked).
    /// </summary>
    public User? BlockedByAdmin { get; set; }

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

            // Get audit log
            AuditLog = await _userManagementService.GetUserAuditLogAsync(id, 20);

            // Get blocked by admin info if user is blocked
            if (User.Status == AccountStatus.Blocked && User.BlockedByUserId.HasValue)
            {
                BlockedByAdmin = await _userManagementService.GetUserDetailsAsync(User.BlockedByUserId.Value);
            }

            // Log sensitive access to user profile
            var adminUserIdClaim = base.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(adminUserIdClaim, out var adminUserId))
            {
                await _auditLogService.LogSensitiveAccessAsync(
                    adminUserId,
                    "UserProfile",
                    id,
                    User.Email,
                    id);
            }

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
