using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Account;

public class VerifyEmailModel : PageModel
{
    private readonly IEmailVerificationService _verificationService;

    public VerifyEmailModel(IEmailVerificationService verificationService)
    {
        _verificationService = verificationService;
    }

    public bool VerificationSuccessful { get; set; }
    public bool TokenExpired { get; set; }
    public string? ErrorMessage { get; set; }
    public string? UserEmail { get; set; }
    public bool ResendSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            ErrorMessage = "No verification token was provided.";
            return Page();
        }

        var result = await _verificationService.VerifyEmailAsync(token);

        if (result.Success)
        {
            VerificationSuccessful = true;
        }
        else
        {
            TokenExpired = result.TokenExpired;
            ErrorMessage = result.ErrorMessage;
            UserEmail = result.User?.Email;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostResendVerificationAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ErrorMessage = "Email address is required to resend verification.";
            return Page();
        }

        await _verificationService.GenerateNewVerificationTokenAsync(email);
        
        // Always show success to prevent user enumeration
        ResendSuccess = true;
        TokenExpired = true;
        UserEmail = email;
        
        return Page();
    }
}
