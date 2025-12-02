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
public class OrderDetailsModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderDetailsModel> _logger;

    public OrderDetailsModel(
        IOrderService orderService,
        ApplicationDbContext context,
        ILogger<OrderDetailsModel> logger)
    {
        _orderService = orderService;
        _context = context;
        _logger = logger;
    }

    public SellerSubOrder? SubOrder { get; set; }
    public Store? CurrentStore { get; set; }

    public async Task<IActionResult> OnGetAsync(int subOrderId)
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

        // Get the sub-order
        SubOrder = await _orderService.GetSubOrderByIdAsync(subOrderId);

        if (SubOrder == null)
        {
            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToPage("/Seller/Orders");
        }

        // Verify that this sub-order belongs to the current seller's store
        if (SubOrder.StoreId != CurrentStore.Id)
        {
            TempData["ErrorMessage"] = "You don't have permission to view this order.";
            return RedirectToPage("/Seller/Orders");
        }

        return Page();
    }
}
