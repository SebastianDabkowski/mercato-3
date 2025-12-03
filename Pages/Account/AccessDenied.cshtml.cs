using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Account;

/// <summary>
/// Page model for the Access Denied page.
/// </summary>
public class AccessDeniedModel : PageModel
{
    private readonly ILogger<AccessDeniedModel> _logger;

    public AccessDeniedModel(ILogger<AccessDeniedModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the return URL that was requested.
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Gets or sets the required role for the resource.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? RequiredRole { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        // Log the access denied event
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        _logger.LogWarning(
            "Access denied - UserId: {UserId}, Email: {Email}, Role: {Role}, RequiredRole: {RequiredRole}, RequestedUrl: {ReturnUrl}",
            userId ?? "not authenticated",
            userEmail ?? "unknown",
            userRole ?? "none",
            RequiredRole ?? "not specified",
            returnUrl ?? "not specified");
    }
}
