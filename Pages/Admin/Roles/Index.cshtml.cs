using MercatoApp.Authorization;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Roles;

/// <summary>
/// Page model for viewing roles and their assigned permissions.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class IndexModel : PageModel
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IPermissionService permissionService,
        ILogger<IndexModel> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of roles with their permissions.
    /// </summary>
    public List<RoleWithPermissions> RolesWithPermissions { get; set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            RolesWithPermissions = await _permissionService.GetRolesWithPermissionsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading roles and permissions");
            RolesWithPermissions = new();
        }
    }
}
