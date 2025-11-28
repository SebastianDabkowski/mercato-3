using Microsoft.AspNetCore.Authorization;

namespace MercatoApp.Authorization;

/// <summary>
/// Authorization requirement for role-based access control.
/// </summary>
public class RoleRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the allowed roles for this requirement.
    /// </summary>
    public string[] AllowedRoles { get; }

    /// <summary>
    /// Creates a new role requirement with the specified allowed roles.
    /// </summary>
    /// <param name="allowedRoles">The roles that are allowed.</param>
    public RoleRequirement(params string[] allowedRoles)
    {
        AllowedRoles = allowedRoles ?? throw new ArgumentNullException(nameof(allowedRoles));
    }
}
