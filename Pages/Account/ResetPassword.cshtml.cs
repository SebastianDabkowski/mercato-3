using System.ComponentModel.DataAnnotations;
using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Account;

public class ResetPasswordModel : PageModel
{
    private readonly IPasswordResetService _passwordResetService;

    public ResetPasswordModel(IPasswordResetService passwordResetService)
    {
        _passwordResetService = passwordResetService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool ResetSuccessful { get; set; }
    public bool TokenExpired { get; set; }
    public bool TokenInvalid { get; set; }

    public class InputModel
    {
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            TokenInvalid = true;
            return Page();
        }

        // Validate the token before showing the form
        var validationResult = await _passwordResetService.ValidateResetTokenAsync(token);
        if (!validationResult.IsValid)
        {
            TokenExpired = validationResult.TokenExpired;
            TokenInvalid = validationResult.TokenNotFound;
            return Page();
        }

        Input.Token = token;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.Token))
        {
            TokenInvalid = true;
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _passwordResetService.ResetPasswordAsync(Input.Token, Input.Password);

        if (!result.Success)
        {
            if (result.TokenExpired)
            {
                TokenExpired = true;
                return Page();
            }

            if (result.TokenInvalid)
            {
                TokenInvalid = true;
                return Page();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            return Page();
        }

        ResetSuccessful = true;
        return Page();
    }
}
