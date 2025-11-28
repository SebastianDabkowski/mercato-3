using System.ComponentModel.DataAnnotations;
using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly IPasswordResetService _passwordResetService;

    public ForgotPasswordModel(IPasswordResetService passwordResetService)
    {
        _passwordResetService = passwordResetService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool RequestSubmitted { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _passwordResetService.RequestPasswordResetAsync(Input.Email);

        // Always show success message to prevent email enumeration
        RequestSubmitted = true;
        return Page();
    }
}
