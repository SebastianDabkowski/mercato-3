using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Test scenario for verifying RBAC configuration.
/// </summary>
public static class RbacTestScenario
{
    /// <summary>
    /// Runs the RBAC test scenario.
    /// </summary>
    public static async Task RunTestAsync(ApplicationDbContext context, IPermissionService permissionService)
    {
        Console.WriteLine("\n=== RBAC Test Scenario ===\n");

        // Test 1: Verify roles exist
        Console.WriteLine("Test 1: Verify Roles");
        var roles = await permissionService.GetAllRolesAsync();
        Console.WriteLine($"  Total roles: {roles.Count}");
        foreach (var role in roles)
        {
            Console.WriteLine($"  - {role.Name}: {role.Description}");
        }
        Console.WriteLine();

        // Test 2: Verify permissions exist
        Console.WriteLine("Test 2: Verify Permissions");
        var permissions = await permissionService.GetAllPermissionsAsync();
        Console.WriteLine($"  Total permissions: {permissions.Count}");
        var permissionsByModule = await permissionService.GetPermissionsByModuleAsync();
        foreach (var module in permissionsByModule.OrderBy(m => m.Key))
        {
            Console.WriteLine($"  {module.Key}: {module.Value.Count} permissions");
        }
        Console.WriteLine();

        // Test 3: Verify role-permission mappings
        Console.WriteLine("Test 3: Verify Role-Permission Mappings");
        var rolesWithPermissions = await permissionService.GetRolesWithPermissionsAsync();
        foreach (var roleWithPerms in rolesWithPermissions)
        {
            Console.WriteLine($"  {roleWithPerms.Role.Name}: {roleWithPerms.Permissions.Count} permissions");
            var moduleGroups = roleWithPerms.Permissions.GroupBy(p => p.Module);
            foreach (var moduleGroup in moduleGroups.OrderBy(g => g.Key))
            {
                Console.WriteLine($"    - {moduleGroup.Key}: {moduleGroup.Count()} permissions");
            }
        }
        Console.WriteLine();

        // Test 4: Verify default role permissions (simplified to avoid EF tracking issues)
        Console.WriteLine("Test 4: Verify Default Role Permissions");
        
        // Buyer should have ViewProducts permission
        var buyerRole = roles.FirstOrDefault(r => r.Name == Role.RoleNames.Buyer);
        if (buyerRole != null)
        {
            var hasBuyerPerm = await permissionService.RoleHasPermissionAsync(
                buyerRole.Id, 
                Permission.PermissionNames.ViewProducts);
            Console.WriteLine($"  Buyer has ViewProducts: {hasBuyerPerm}");
        }

        // Seller should have CreateProducts permission
        var sellerRole = roles.FirstOrDefault(r => r.Name == Role.RoleNames.Seller);
        if (sellerRole != null)
        {
            var hasSellerPerm = await permissionService.RoleHasPermissionAsync(
                sellerRole.Id, 
                Permission.PermissionNames.CreateProducts);
            Console.WriteLine($"  Seller has CreateProducts: {hasSellerPerm}");
        }

        // Admin should have ManageRoles permission
        var adminRole = roles.FirstOrDefault(r => r.Name == Role.RoleNames.Admin);
        if (adminRole != null)
        {
            var hasAdminPerm = await permissionService.RoleHasPermissionAsync(
                adminRole.Id, 
                Permission.PermissionNames.ManageRoles);
            Console.WriteLine($"  Admin has ManageRoles: {hasAdminPerm}");
        }

        // Compliance should have AccessAuditLogs permission
        var complianceRole = roles.FirstOrDefault(r => r.Name == Role.RoleNames.Compliance);
        if (complianceRole != null)
        {
            var hasCompliancePerm = await permissionService.RoleHasPermissionAsync(
                complianceRole.Id, 
                Permission.PermissionNames.AccessAuditLogs);
            Console.WriteLine($"  Compliance has AccessAuditLogs: {hasCompliancePerm}");
        }

        Console.WriteLine("\n=== RBAC Test Scenario Complete ===\n");
    }
}
