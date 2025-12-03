using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing role permissions.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        ApplicationDbContext context,
        ILogger<PermissionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<RoleWithPermissions>> GetRolesWithPermissionsAsync()
    {
        var roles = await _context.Roles
            .Where(r => r.IsActive)
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .ToListAsync();

        return roles.Select(r => new RoleWithPermissions
        {
            Role = r,
            Permissions = r.RolePermissions
                .Where(rp => rp.IsActive && rp.Permission.IsActive)
                .Select(rp => rp.Permission)
                .ToList()
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<List<Permission>> GetRolePermissionsAsync(int roleId)
    {
        return await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId && rp.IsActive)
            .Include(rp => rp.Permission)
            .Where(rp => rp.Permission.IsActive)
            .Select(rp => rp.Permission)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Permission>> GetAllPermissionsAsync()
    {
        return await _context.Permissions
            .Where(p => p.IsActive)
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, List<Permission>>> GetPermissionsByModuleAsync()
    {
        var permissions = await GetAllPermissionsAsync();
        return permissions
            .GroupBy(p => p.Module)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <inheritdoc />
    public async Task<PermissionResult> AssignPermissionToRoleAsync(int roleId, int permissionId, int grantedByUserId)
    {
        try
        {
            // Verify role exists
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
            {
                return PermissionResult.FailResult("Role not found.");
            }

            // Verify permission exists
            var permission = await _context.Permissions.FindAsync(permissionId);
            if (permission == null)
            {
                return PermissionResult.FailResult("Permission not found.");
            }

            // Check if mapping already exists
            var existingMapping = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (existingMapping != null)
            {
                // If exists but inactive, reactivate it
                if (!existingMapping.IsActive)
                {
                    existingMapping.IsActive = true;
                    existingMapping.ModifiedAt = DateTime.UtcNow;
                    existingMapping.ModifiedByUserId = grantedByUserId;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Permission {PermissionName} reactivated for role {RoleName} by user {UserId}",
                        permission.Name, role.Name, grantedByUserId);

                    return PermissionResult.SuccessResult();
                }

                return PermissionResult.FailResult("Permission already assigned to this role.");
            }

            // Create new mapping
            var rolePermission = new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                GrantedByUserId = grantedByUserId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.RolePermissions.Add(rolePermission);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Permission {PermissionName} assigned to role {RoleName} by user {UserId}",
                permission.Name, role.Name, grantedByUserId);

            return PermissionResult.SuccessResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission {PermissionId} to role {RoleId}", permissionId, roleId);
            return PermissionResult.FailResult("An error occurred while assigning the permission.");
        }
    }

    /// <inheritdoc />
    public async Task<PermissionResult> RevokePermissionFromRoleAsync(int roleId, int permissionId, int revokedByUserId)
    {
        try
        {
            var rolePermission = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (rolePermission == null)
            {
                return PermissionResult.FailResult("Permission not assigned to this role.");
            }

            if (!rolePermission.IsActive)
            {
                return PermissionResult.FailResult("Permission already revoked from this role.");
            }

            // Soft delete by setting IsActive to false
            rolePermission.IsActive = false;
            rolePermission.ModifiedAt = DateTime.UtcNow;
            rolePermission.ModifiedByUserId = revokedByUserId;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Permission {PermissionName} revoked from role {RoleName} by user {UserId}",
                rolePermission.Permission.Name, rolePermission.Role.Name, revokedByUserId);

            return PermissionResult.SuccessResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking permission {PermissionId} from role {RoleId}", permissionId, roleId);
            return PermissionResult.FailResult("An error occurred while revoking the permission.");
        }
    }

    /// <inheritdoc />
    public async Task<bool> RoleHasPermissionAsync(int roleId, string permissionName)
    {
        return await _context.RolePermissions
            .Include(rp => rp.Permission)
            .AnyAsync(rp => rp.RoleId == roleId && 
                           rp.Permission.Name == permissionName && 
                           rp.IsActive && 
                           rp.Permission.IsActive);
    }

    /// <inheritdoc />
    public async Task<List<Role>> GetAllRolesAsync()
    {
        return await _context.Roles
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }
}
