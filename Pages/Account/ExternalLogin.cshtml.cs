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
    private readonly ISessionService _sessionService;
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly ILogger<ExternalLoginModel> _logger;

    public ExternalLoginModel(
        ISocialLoginService socialLoginService,
        ISessionService sessionService,
        IAuthenticationSchemeProvider schemeProvider,
        ILogger<ExternalLoginModel> logger)
    {
        _socialLoginService = socialLoginService;
        _sessionService = sessionService;
        _schemeProvider = schemeProvider;
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

        // Get all configured external authentication schemes
        var schemes = await _schemeProvider.GetAllSchemesAsync();
        var externalSchemes = schemes
            .Where(s => !string.IsNullOrEmpty(s.DisplayName) && 
                       s.Name != CookieAuthenticationDefaults.AuthenticationScheme)
            .Select(s => s.Name)
            .ToList();

        AuthenticateResult? externalResult = null;
        string? authenticatedScheme = null;

        // Try each configured external scheme until one succeeds
        foreach (var scheme in externalSchemes)
        {
            try
            {
                var result = await HttpContext.AuthenticateAsync(scheme);
                if (result.Succeeded)
                {
                    externalResult = result;
                    authenticatedScheme = scheme;
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Authentication attempt with scheme {Scheme} failed", scheme);
            }
        }

        if (externalResult?.Succeeded != true)
        {
            _logger.LogWarning("External authentication failed - no scheme succeeded");
            ErrorMessage = "External authentication failed. Please try again.";
            return Page();
        }

        var externalPrincipal = externalResult.Principal;
        var provider = externalResult.Properties?.Items["LoginProvider"] 
            ?? authenticatedScheme
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

        var loginResult = await _socialLoginService.AuthenticateOrRegisterBuyerAsync(externalInfo);

        if (!loginResult.Success)
        {
            ErrorMessage = loginResult.ErrorMessage ?? "Social login failed. Please try again.";
            return Page();
        }

        // Sign out the external cookie
        await HttpContext.SignOutAsync(normalizedProvider);

        // Create a secure session token
        var sessionData = new SessionCreationData
        {
            UserId = loginResult.User!.Id,
            SecurityStamp = loginResult.User.SecurityStamp,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            IsPersistent = true // Social logins default to persistent sessions
        };

        var sessionResult = await _sessionService.CreateSessionAsync(sessionData);

        if (!sessionResult.Success)
        {
            ErrorMessage = "Failed to create session. Please try again.";
            return Page();
        }

        // Create claims for the authenticated user, including the session token
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, loginResult.User!.Id.ToString()),
            new(ClaimTypes.Email, loginResult.User.Email),
            new(ClaimTypes.Name, $"{loginResult.User.FirstName} {loginResult.User.LastName}".Trim()),
            new(ClaimTypes.GivenName, loginResult.User.FirstName),
            new(ClaimTypes.Surname, loginResult.User.LastName),
            new(ClaimTypes.Role, loginResult.User.UserType.ToString()),
            new("ExternalProvider", normalizedProvider),
            new("SessionToken", sessionResult.Token!) // Store session token in claims for validation
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.Add(_sessionService.PersistentSessionDuration)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation(
            "User {Email} logged in with {Provider} (IsNewUser: {IsNewUser})",
            loginResult.User.Email,
            normalizedProvider,
            loginResult.IsNewUser);

        return LocalRedirect(returnUrl);
    }
}
