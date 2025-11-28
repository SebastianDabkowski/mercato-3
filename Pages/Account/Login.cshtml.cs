using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Account;

public class LoginModel : PageModel
{
    private readonly IUserAuthenticationService _authenticationService;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly IConfiguration _configuration;

    public LoginModel(
        IUserAuthenticationService authenticationService,
        IEmailVerificationService emailVerificationService,
        IConfiguration configuration)
    {
        _authenticationService = authenticationService;
        _emailVerificationService = emailVerificationService;
        _configuration = configuration;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public bool ShowResendVerification { get; set; }

    public string? SocialLoginError { get; set; }

    public List<string> ExternalProviders { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public void OnGet(string? returnUrl = null, string? error = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
        SocialLoginError = error;
        LoadExternalProviders();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
        LoadExternalProviders();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var loginData = new LoginData
        {
            Email = Input.Email,
            Password = Input.Password
        };

        var result = await _authenticationService.AuthenticateAsync(loginData);

        if (!result.Success)
        {
            if (result.RequiresEmailVerification)
            {
                ShowResendVerification = true;
            }
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Login failed.");
            return Page();
        }

        // Create claims for the authenticated user
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.User!.Id.ToString()),
            new(ClaimTypes.Email, result.User.Email),
            new(ClaimTypes.Name, $"{result.User.FirstName} {result.User.LastName}"),
            new(ClaimTypes.GivenName, result.User.FirstName),
            new(ClaimTypes.Surname, result.User.LastName),
            new(ClaimTypes.Role, result.User.UserType.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = Input.RememberMe,
            ExpiresUtc = Input.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        // Check if seller requires KYC - redirect to KYC page if not approved
        if (result.User.UserType == UserType.Seller && result.User.KycStatus != KycStatus.Approved)
        {
            return RedirectToPage("/Account/KycRequired");
        }

        // Redirect to home (role-based dashboard routing can be added when dashboards exist)
        return LocalRedirect(ReturnUrl);
    }

    public async Task<IActionResult> OnPostResendVerificationAsync()
    {
        LoadExternalProviders();

        if (string.IsNullOrEmpty(Input.Email))
        {
            ModelState.AddModelError("Input.Email", "Please enter your email address.");
            ShowResendVerification = true;
            return Page();
        }

        // Generate new verification token using the verification service
        await _emailVerificationService.GenerateNewVerificationTokenAsync(Input.Email);
        
        // Always show success message to prevent user enumeration
        TempData["Message"] = "If your email is registered, a verification link has been sent.";
        return RedirectToPage();
    }

    private void LoadExternalProviders()
    {
        // Check if Google is configured
        var googleClientId = _configuration["Authentication:Google:ClientId"];
        var googleClientSecret = _configuration["Authentication:Google:ClientSecret"];
        if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
        {
            ExternalProviders.Add("Google");
        }

        // Check if Facebook is configured
        var facebookAppId = _configuration["Authentication:Facebook:AppId"];
        var facebookAppSecret = _configuration["Authentication:Facebook:AppSecret"];
        if (!string.IsNullOrEmpty(facebookAppId) && !string.IsNullOrEmpty(facebookAppSecret))
        {
            ExternalProviders.Add("Facebook");
        }
    }
}
