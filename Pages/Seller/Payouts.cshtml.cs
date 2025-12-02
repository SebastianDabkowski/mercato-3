using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MercatoApp.Pages.Seller;

[Authorize(Policy = "SellerOnly")]
public class PayoutsModel : PageModel
{
    private readonly IPayoutService _payoutService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PayoutsModel> _logger;

    public PayoutsModel(
        IPayoutService payoutService,
        ApplicationDbContext context,
        ILogger<PayoutsModel> logger)
    {
        _payoutService = payoutService;
        _context = context;
        _logger = logger;
    }

    public List<Payout> Payouts { get; set; } = new();
    public Store? CurrentStore { get; set; }

    // Filter properties
    [BindProperty(SupportsGet = true)]
    public PayoutStatus? SelectedStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Account/Login");
        }

        // Get the seller's store
        CurrentStore = await _context.Stores
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (CurrentStore == null)
        {
            TempData["ErrorMessage"] = "Store not found.";
            return RedirectToPage("/Index");
        }

        // Get all payouts for the store
        var payouts = await _payoutService.GetPayoutsAsync(CurrentStore.Id, SelectedStatus);

        // Apply date filters
        if (FromDate.HasValue)
        {
            payouts = payouts.Where(p => p.CreatedAt.Date >= FromDate.Value.Date).ToList();
        }

        if (ToDate.HasValue)
        {
            payouts = payouts.Where(p => p.CreatedAt.Date <= ToDate.Value.Date).ToList();
        }

        Payouts = payouts;

        return Page();
    }
}
