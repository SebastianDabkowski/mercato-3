using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MercatoApp.Data;

namespace MercatoApp.Pages.Seller;

[Authorize(Policy = "SellerOnly")]
public class OrdersModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrdersModel> _logger;

    public OrdersModel(
        IOrderService orderService,
        ApplicationDbContext context,
        ILogger<OrdersModel> logger)
    {
        _orderService = orderService;
        _context = context;
        _logger = logger;
    }

    public List<SellerSubOrder> SubOrders { get; set; } = new();
    public Store? CurrentStore { get; set; }

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

        // Get all sub-orders for this store
        SubOrders = await _orderService.GetSubOrdersByStoreIdAsync(CurrentStore.Id);

        return Page();
    }
}
