using MercatoApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly ISessionService _sessionService;

    public LogoutModel(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Invalidate the session token in the database
        var sessionToken = User.FindFirst("SessionToken")?.Value;
        if (!string.IsNullOrEmpty(sessionToken))
        {
            await _sessionService.InvalidateSessionAsync(sessionToken);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Index");
    }
}
