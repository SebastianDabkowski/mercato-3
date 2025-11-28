using System.Security.Claims;
using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Pages.Account;

[Authorize]
public class KycRequiredModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<KycRequiredModel> _logger;

    public KycRequiredModel(ApplicationDbContext context, ILogger<KycRequiredModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public KycStatus KycStatus { get; set; }
    public DateTime? KycSubmittedAt { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var id))
        {
            return RedirectToPage("/Account/Login");
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return RedirectToPage("/Account/Login");
        }

        // Only sellers need KYC
        if (user.UserType != UserType.Seller)
        {
            return RedirectToPage("/Index");
        }

        // If KYC is already approved, redirect to dashboard
        if (user.KycStatus == KycStatus.Approved)
        {
            return RedirectToPage("/Index");
        }

        KycStatus = user.KycStatus;
        KycSubmittedAt = user.KycSubmittedAt;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var id))
        {
            return RedirectToPage("/Account/Login");
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return RedirectToPage("/Account/Login");
        }

        // Only sellers can start KYC
        if (user.UserType != UserType.Seller)
        {
            return RedirectToPage("/Index");
        }

        // Start KYC process - in a real implementation, this would redirect to a KYC provider
        // For now, we simulate starting the KYC process by setting status to Pending
        user.KycStatus = KycStatus.Pending;
        user.KycSubmittedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("KYC verification started for seller: {Email}", user.Email);

        return RedirectToPage();
    }
}
