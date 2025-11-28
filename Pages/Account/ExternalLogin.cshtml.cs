using System.Security.Claims;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Account;

public class ExternalLoginModel : PageModel
{
    private readonly ISocialLoginService _socialLoginService;
    private readonly ILogger<ExternalLoginModel> _logger;

    public ExternalLoginModel(
        ISocialLoginService socialLoginService,
        ILogger<ExternalLoginModel> logger)
    {
        _socialLoginService = socialLoginService;
        _logger = logger;
    }

    public string? ErrorMessage { get; set; }

    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Initiates the external login challenge.
    /// </summary>
    public IActionResult OnGet(string provider, string? returnUrl = null)
    {
        // Validate provider
        if (string.IsNullOrEmpty(provider))
        {
            return RedirectToPage("/Account/Login");
        }

        var validProviders = new[] { "Google", "Facebook" };
        if (!validProviders.Contains(provider, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Invalid external login provider requested: {Provider}", provider);
            return RedirectToPage("/Account/Login");
        }

        // Request a redirect to the external login provider
        var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl,
            Items =
            {
                { "LoginProvider", provider }
            }
        };

        return Challenge(properties, provider);
    }

    /// <summary>
    /// Handles the callback from the external login provider.
    /// </summary>
    public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!string.IsNullOrEmpty(remoteError))
        {
            _logger.LogWarning("External login error: {Error}", remoteError);
            ErrorMessage = $"Error from external provider: {remoteError}";
            return Page();
        }

        // Get the external login info
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        // For external authentication, we need to check the external scheme
        // First, try to get info from the temporary cookie set during the OAuth flow
        AuthenticateResult? externalResult = null;
        
        // Try Google first, then Facebook
        try
        {
            externalResult = await HttpContext.AuthenticateAsync("Google");
            if (!externalResult.Succeeded)
            {
                externalResult = await HttpContext.AuthenticateAsync("Facebook");
            }
        }
        catch
        {
            // Provider might not be configured
            try
            {
                externalResult = await HttpContext.AuthenticateAsync("Facebook");
            }
            catch
            {
                // Neither provider is configured
            }
        }

        if (externalResult?.Succeeded != true)
        {
            _logger.LogWarning("External authentication failed");
            ErrorMessage = "External authentication failed. Please try again.";
            return Page();
        }

        var externalPrincipal = externalResult.Principal;
        var provider = externalResult.Properties?.Items["LoginProvider"] 
            ?? externalPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Issuer 
            ?? "Unknown";

        // Extract user info from claims
        var providerId = externalPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = externalPrincipal?.FindFirst(ClaimTypes.Email)?.Value;
        var firstName = externalPrincipal?.FindFirst(ClaimTypes.GivenName)?.Value 
            ?? externalPrincipal?.FindFirst("first_name")?.Value;
        var lastName = externalPrincipal?.FindFirst(ClaimTypes.Surname)?.Value 
            ?? externalPrincipal?.FindFirst("last_name")?.Value;

        // If name claims are not available, try to split the full name
        if (string.IsNullOrEmpty(firstName))
        {
            var fullName = externalPrincipal?.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrEmpty(fullName))
            {
                var nameParts = fullName.Split(' ', 2);
                firstName = nameParts[0];
                lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
            }
        }

        if (string.IsNullOrEmpty(providerId) || string.IsNullOrEmpty(email))
        {
            _logger.LogWarning(
                "Missing required claims from external provider. ProviderId: {HasProviderId}, Email: {HasEmail}",
                !string.IsNullOrEmpty(providerId),
                !string.IsNullOrEmpty(email));
            ErrorMessage = "Could not retrieve required information from the external provider. Please ensure you grant access to your email.";
            return Page();
        }

        // Normalize provider name
        var normalizedProvider = provider switch
        {
            "Google" or "google" => "Google",
            "Facebook" or "facebook" => "Facebook",
            _ => provider
        };

        // Authenticate or register the buyer
        var externalInfo = new ExternalUserInfo
        {
            Provider = normalizedProvider,
            ProviderId = providerId,
            Email = email,
            FirstName = firstName,
            LastName = lastName
        };

        var result = await _socialLoginService.AuthenticateOrRegisterBuyerAsync(externalInfo);

        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage ?? "Social login failed. Please try again.";
            return Page();
        }

        // Sign out the external cookie
        await HttpContext.SignOutAsync(normalizedProvider);

        // Create claims for the authenticated user
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.User!.Id.ToString()),
            new(ClaimTypes.Email, result.User.Email),
            new(ClaimTypes.Name, $"{result.User.FirstName} {result.User.LastName}".Trim()),
            new(ClaimTypes.GivenName, result.User.FirstName),
            new(ClaimTypes.Surname, result.User.LastName),
            new(ClaimTypes.Role, result.User.UserType.ToString()),
            new("ExternalProvider", normalizedProvider)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation(
            "User {Email} logged in with {Provider} (IsNewUser: {IsNewUser})",
            result.User.Email,
            normalizedProvider,
            result.IsNewUser);

        return LocalRedirect(returnUrl);
    }
}
