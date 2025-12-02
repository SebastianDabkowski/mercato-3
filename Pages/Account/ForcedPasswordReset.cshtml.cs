using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using MercatoApp.Data;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Pages.Account;

/// <summary>
/// Page model for forced password reset after account reactivation.
/// </summary>
public class ForcedPasswordResetModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordResetService _passwordResetService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<ForcedPasswordResetModel> _logger;

    public ForcedPasswordResetModel(
        ApplicationDbContext context,
        IPasswordResetService passwordResetService,
        ISessionService sessionService,
        ILogger<ForcedPasswordResetModel> logger)
    {
        _context = context;
        _passwordResetService = passwordResetService;
        _sessionService = sessionService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool ChangeSuccessful { get; set; }

    public string UserEmail { get; set; } = string.Empty;

    public class InputModel
    {
        [Required(ErrorMessage = "Current password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public int UserId { get; set; }
    }

    /// <summary>
    /// Handles GET request to display forced password reset page.
    /// </summary>
    /// <param name="userId">The user ID requiring password reset.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToPage("/Account/Login");
        }

        // Verify that this user actually requires a password reset
        if (!user.RequirePasswordReset)
        {
            TempData["ErrorMessage"] = "Password reset is not required for this account.";
            return RedirectToPage("/Account/Login");
        }

        Input.UserId = userId;
        UserEmail = user.Email;
        return Page();
    }

    /// <summary>
    /// Handles POST request to change the password.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _context.Users.FindAsync(Input.UserId);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "User not found.");
            return Page();
        }

        UserEmail = user.Email;

        // Verify that this user actually requires a password reset
        if (!user.RequirePasswordReset)
        {
            TempData["ErrorMessage"] = "Password reset is not required for this account.";
            return RedirectToPage("/Account/Login");
        }

        var result = await _passwordResetService.ChangePasswordAsync(
            Input.UserId, 
            Input.CurrentPassword, 
            Input.NewPassword);

        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            return Page();
        }

        // Clear the RequirePasswordReset flag
        user.RequirePasswordReset = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} completed forced password reset", Input.UserId);

        // Invalidate all user sessions (security stamp change will also invalidate on next validation)
        await _sessionService.InvalidateAllUserSessionsAsync(Input.UserId);

        // Sign out any existing session and redirect to login
        if (User.Identity?.IsAuthenticated == true)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        
        TempData["SuccessMessage"] = "Your password has been changed successfully. Please log in with your new password.";
        return RedirectToPage("/Account/Login");
    }
}
