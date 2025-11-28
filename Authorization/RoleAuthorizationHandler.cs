using System.Security.Claims;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;

namespace MercatoApp.Authorization;

/// <summary>
/// Authorization handler for role-based access control.
/// </summary>
public class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
{
    private readonly IRoleAuthorizationService _roleAuthorizationService;

    public RoleAuthorizationHandler(IRoleAuthorizationService roleAuthorizationService)
    {
        _roleAuthorizationService = roleAuthorizationService;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        var result = _roleAuthorizationService.AuthorizeAnyRole(context.User, requirement.AllowedRoles);

        if (result.IsAuthorized)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
