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
    private readonly IEmailService _emailService;

    public LoginModel(
        IUserAuthenticationService authenticationService,
        IEmailService emailService)
    {
        _authenticationService = authenticationService;
        _emailService = emailService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public bool ShowResendVerification { get; set; }

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

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");

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

        // Redirect based on user role
        return result.User.UserType == UserType.Seller
            ? LocalRedirect(Url.Content("~/"))  // Future: redirect to seller dashboard
            : LocalRedirect(ReturnUrl);
    }

    public async Task<IActionResult> OnPostResendVerificationAsync()
    {
        if (string.IsNullOrEmpty(Input.Email))
        {
            ModelState.AddModelError("Input.Email", "Please enter your email address.");
            ShowResendVerification = true;
            return Page();
        }

        // Note: In production, you would look up the user and regenerate/resend the token
        // For security, we show a success message regardless of whether the email exists
        await _emailService.ResendVerificationEmailAsync(Input.Email, "resend-token-placeholder");
        
        TempData["Message"] = "If your email is registered, a verification link has been sent.";
        return RedirectToPage();
    }
}
