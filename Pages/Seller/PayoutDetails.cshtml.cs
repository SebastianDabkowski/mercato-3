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
public class PayoutDetailsModel : PageModel
{
    private readonly IPayoutService _payoutService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PayoutDetailsModel> _logger;

    public PayoutDetailsModel(
        IPayoutService payoutService,
        ApplicationDbContext context,
        ILogger<PayoutDetailsModel> logger)
    {
        _payoutService = payoutService;
        _context = context;
        _logger = logger;
    }

    public Payout? Payout { get; set; }
    public Store? CurrentStore { get; set; }
    public List<EscrowTransaction> EscrowTransactions { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int PayoutId { get; set; }

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

        // Get the payout with details
        Payout = await _payoutService.GetPayoutAsync(PayoutId);

        if (Payout == null)
        {
            TempData["ErrorMessage"] = "Payout not found.";
            return RedirectToPage("/Seller/Payouts");
        }

        // Verify the payout belongs to the current store
        if (Payout.StoreId != CurrentStore.Id)
        {
            TempData["ErrorMessage"] = "You don't have permission to view this payout.";
            return RedirectToPage("/Seller/Payouts");
        }

        // Get escrow transactions with related order information
        EscrowTransactions = await _context.EscrowTransactions
            .Where(e => e.PayoutId == PayoutId)
            .Include(e => e.SellerSubOrder)
                .ThenInclude(s => s.ParentOrder)
                    .ThenInclude(o => o.User)
            .Include(e => e.SellerSubOrder)
                .ThenInclude(s => s.Items)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return Page();
    }
}
