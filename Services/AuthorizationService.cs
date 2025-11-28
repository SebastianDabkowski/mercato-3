using System.Security.Claims;
using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Result of an authorization check.
/// </summary>
public class RoleAuthorizationResult
{
    /// <summary>
    /// Gets or sets whether the authorization was successful.
    /// </summary>
    public bool IsAuthorized { get; set; }

    /// <summary>
    /// Gets or sets the reason for authorization failure.
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Creates a successful authorization result.
    /// </summary>
    public static RoleAuthorizationResult Success() => new() { IsAuthorized = true };

    /// <summary>
    /// Creates a failed authorization result with the specified reason.
    /// </summary>
    public static RoleAuthorizationResult Fail(string reason) => new() { IsAuthorized = false, FailureReason = reason };
}

/// <summary>
/// Interface for centralized role-based authorization service.
/// </summary>
public interface IRoleAuthorizationService
{
    /// <summary>
    /// Checks if the user has the specified role.
    /// </summary>
    /// <param name="user">The claims principal representing the user.</param>
    /// <param name="role">The role to check.</param>
    /// <returns>The authorization result.</returns>
    RoleAuthorizationResult AuthorizeRole(ClaimsPrincipal user, string role);

    /// <summary>
    /// Checks if the user has any of the specified roles.
    /// </summary>
    /// <param name="user">The claims principal representing the user.</param>
    /// <param name="roles">The roles to check.</param>
    /// <returns>The authorization result.</returns>
    RoleAuthorizationResult AuthorizeAnyRole(ClaimsPrincipal user, params string[] roles);

    /// <summary>
    /// Checks if the user is a buyer.
    /// </summary>
    /// <param name="user">The claims principal representing the user.</param>
    /// <returns>The authorization result.</returns>
    RoleAuthorizationResult AuthorizeBuyer(ClaimsPrincipal user);

    /// <summary>
    /// Checks if the user is a seller.
    /// </summary>
    /// <param name="user">The claims principal representing the user.</param>
    /// <returns>The authorization result.</returns>
    RoleAuthorizationResult AuthorizeSeller(ClaimsPrincipal user);

    /// <summary>
    /// Checks if the user is an admin.
    /// </summary>
    /// <param name="user">The claims principal representing the user.</param>
    /// <returns>The authorization result.</returns>
    RoleAuthorizationResult AuthorizeAdmin(ClaimsPrincipal user);
}

/// <summary>
/// Service for centralized role-based authorization with logging.
/// </summary>
public class RoleAuthorizationService : IRoleAuthorizationService
{
    private readonly ILogger<RoleAuthorizationService> _logger;

    public RoleAuthorizationService(ILogger<RoleAuthorizationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public RoleAuthorizationResult AuthorizeRole(ClaimsPrincipal user, string role)
    {
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            LogAuthorizationFailure(user, role, "User is not authenticated");
            return RoleAuthorizationResult.Fail("User is not authenticated.");
        }

        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(userRole))
        {
            LogAuthorizationFailure(user, role, "User has no role assigned");
            return RoleAuthorizationResult.Fail("User has no role assigned.");
        }

        if (!string.Equals(userRole, role, StringComparison.OrdinalIgnoreCase))
        {
            LogAuthorizationFailure(user, role, $"User role '{userRole}' does not match required role '{role}'");
            return RoleAuthorizationResult.Fail($"Access denied. Required role: {role}.");
        }

        return RoleAuthorizationResult.Success();
    }

    /// <inheritdoc />
    public RoleAuthorizationResult AuthorizeAnyRole(ClaimsPrincipal user, params string[] roles)
    {
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            LogAuthorizationFailure(user, string.Join(", ", roles), "User is not authenticated");
            return RoleAuthorizationResult.Fail("User is not authenticated.");
        }

        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(userRole))
        {
            LogAuthorizationFailure(user, string.Join(", ", roles), "User has no role assigned");
            return RoleAuthorizationResult.Fail("User has no role assigned.");
        }

        if (!roles.Any(r => string.Equals(r, userRole, StringComparison.OrdinalIgnoreCase)))
        {
            LogAuthorizationFailure(user, string.Join(", ", roles), $"User role '{userRole}' is not in allowed roles");
            return RoleAuthorizationResult.Fail($"Access denied. Required roles: {string.Join(", ", roles)}.");
        }

        return RoleAuthorizationResult.Success();
    }

    /// <inheritdoc />
    public RoleAuthorizationResult AuthorizeBuyer(ClaimsPrincipal user)
    {
        return AuthorizeRole(user, Role.RoleNames.Buyer);
    }

    /// <inheritdoc />
    public RoleAuthorizationResult AuthorizeSeller(ClaimsPrincipal user)
    {
        return AuthorizeRole(user, Role.RoleNames.Seller);
    }

    /// <inheritdoc />
    public RoleAuthorizationResult AuthorizeAdmin(ClaimsPrincipal user)
    {
        return AuthorizeRole(user, Role.RoleNames.Admin);
    }

    private void LogAuthorizationFailure(ClaimsPrincipal user, string requiredRole, string reason)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var userEmail = user.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value ?? "none";

        _logger.LogWarning(
            "Authorization failure - UserId: {UserId}, Email: {Email}, UserRole: {UserRole}, RequiredRole: {RequiredRole}, Reason: {Reason}",
            userId, userEmail, userRole, requiredRole, reason);
    }
}
