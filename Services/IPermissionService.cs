using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Result of a permission operation.
/// </summary>
public class PermissionResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful permission result.
    /// </summary>
    public static PermissionResult SuccessResult() => new() { Success = true };

    /// <summary>
    /// Creates a failed permission result with the specified error message.
    /// </summary>
    public static PermissionResult FailResult(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Interface for managing role permissions.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Gets all roles with their assigned permissions.
    /// </summary>
    /// <returns>A list of roles with permissions.</returns>
    Task<List<RoleWithPermissions>> GetRolesWithPermissionsAsync();

    /// <summary>
    /// Gets permissions for a specific role.
    /// </summary>
    /// <param name="roleId">The role ID.</param>
    /// <returns>A list of permissions for the role.</returns>
    Task<List<Permission>> GetRolePermissionsAsync(int roleId);

    /// <summary>
    /// Gets all available permissions.
    /// </summary>
    /// <returns>A list of all permissions.</returns>
    Task<List<Permission>> GetAllPermissionsAsync();

    /// <summary>
    /// Gets all available permissions grouped by module.
    /// </summary>
    /// <returns>A dictionary of permissions grouped by module.</returns>
    Task<Dictionary<string, List<Permission>>> GetPermissionsByModuleAsync();

    /// <summary>
    /// Assigns a permission to a role.
    /// </summary>
    /// <param name="roleId">The role ID.</param>
    /// <param name="permissionId">The permission ID.</param>
    /// <param name="grantedByUserId">The ID of the user granting the permission.</param>
    /// <returns>The result of the operation.</returns>
    Task<PermissionResult> AssignPermissionToRoleAsync(int roleId, int permissionId, int grantedByUserId);

    /// <summary>
    /// Revokes a permission from a role.
    /// </summary>
    /// <param name="roleId">The role ID.</param>
    /// <param name="permissionId">The permission ID.</param>
    /// <param name="revokedByUserId">The ID of the user revoking the permission.</param>
    /// <returns>The result of the operation.</returns>
    Task<PermissionResult> RevokePermissionFromRoleAsync(int roleId, int permissionId, int revokedByUserId);

    /// <summary>
    /// Checks if a role has a specific permission.
    /// </summary>
    /// <param name="roleId">The role ID.</param>
    /// <param name="permissionName">The permission name.</param>
    /// <returns>True if the role has the permission, false otherwise.</returns>
    Task<bool> RoleHasPermissionAsync(int roleId, string permissionName);

    /// <summary>
    /// Gets all roles.
    /// </summary>
    /// <returns>A list of all roles.</returns>
    Task<List<Role>> GetAllRolesAsync();
}

/// <summary>
/// Helper class for returning role with permissions data.
/// </summary>
public class RoleWithPermissions
{
    /// <summary>
    /// Gets or sets the role.
    /// </summary>
    public Role Role { get; set; } = null!;

    /// <summary>
    /// Gets or sets the permissions assigned to the role.
    /// </summary>
    public List<Permission> Permissions { get; set; } = new();
}
