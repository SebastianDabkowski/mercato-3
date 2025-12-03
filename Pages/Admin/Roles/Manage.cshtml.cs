using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MercatoApp.Pages.Admin.Roles;

/// <summary>
/// Page model for managing permissions for a specific role.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class ManageModel : PageModel
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<ManageModel> _logger;

    public ManageModel(
        IPermissionService permissionService,
        ILogger<ManageModel> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public int RoleId { get; set; }

    public Role? Role { get; set; }
    public Dictionary<string, List<Permission>> AllPermissionsByModule { get; set; } = new();
    public List<Permission> RolePermissions { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var roles = await _permissionService.GetAllRolesAsync();
        Role = roles.FirstOrDefault(r => r.Id == RoleId);
        
        if (Role == null)
        {
            return NotFound();
        }

        AllPermissionsByModule = await _permissionService.GetPermissionsByModuleAsync();
        RolePermissions = await _permissionService.GetRolePermissionsAsync(RoleId);

        return Page();
    }

    public async Task<IActionResult> OnPostAssignAsync(int permissionId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            ErrorMessage = "User not authenticated.";
            return RedirectToPage();
        }

        var result = await _permissionService.AssignPermissionToRoleAsync(RoleId, permissionId, userId.Value);
        
        if (result.Success)
        {
            SuccessMessage = "Permission assigned successfully.";
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Failed to assign permission.";
        }

        return RedirectToPage(new { roleId = RoleId });
    }

    public async Task<IActionResult> OnPostRevokeAsync(int permissionId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            ErrorMessage = "User not authenticated.";
            return RedirectToPage();
        }

        var result = await _permissionService.RevokePermissionFromRoleAsync(RoleId, permissionId, userId.Value);
        
        if (result.Success)
        {
            SuccessMessage = "Permission revoked successfully.";
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Failed to revoke permission.";
        }

        return RedirectToPage(new { roleId = RoleId });
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            return userId;
        }
        return null;
    }
}
