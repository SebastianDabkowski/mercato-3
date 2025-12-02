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
    private readonly IOrderStatusService _orderStatusService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderDetailsModel> _logger;

    public OrderDetailsModel(
        IOrderService orderService,
        IOrderStatusService orderStatusService,
        ApplicationDbContext context,
        ILogger<OrderDetailsModel> logger)
    {
        _orderService = orderService;
        _orderStatusService = orderStatusService;
        _context = context;
        _logger = logger;
    }

    public SellerSubOrder? SubOrder { get; set; }
    public Store? CurrentStore { get; set; }

    [BindProperty]
    public string? TrackingNumber { get; set; }

    [BindProperty]
    public string? CarrierName { get; set; }

    [BindProperty]
    public string? TrackingUrl { get; set; }

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

    public async Task<IActionResult> OnPostUpdateStatusAsync(int subOrderId, string action)
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
            TempData["ErrorMessage"] = "You don't have permission to update this order.";
            return RedirectToPage("/Seller/Orders");
        }

        // Execute the requested action (pass userId for audit trail)
        (bool success, string? errorMessage) = action switch
        {
            "preparing" => await _orderStatusService.UpdateSubOrderToPreparingAsync(subOrderId, userId),
            "shipped" => await _orderStatusService.UpdateSubOrderToShippedAsync(
                subOrderId, TrackingNumber, CarrierName, TrackingUrl, userId),
            "delivered" => await _orderStatusService.UpdateSubOrderToDeliveredAsync(subOrderId, userId),
            "cancel" => await _orderStatusService.CancelSubOrderAsync(subOrderId, userId),
            _ => (false, "Invalid action.")
        };

        if (success)
        {
            TempData["SuccessMessage"] = "Order status updated successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage ?? "Failed to update order status.";
        }

        return RedirectToPage(new { subOrderId });
    }

    public async Task<IActionResult> OnPostUpdateTrackingAsync(int subOrderId)
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
            TempData["ErrorMessage"] = "You don't have permission to update this order.";
            return RedirectToPage("/Seller/Orders");
        }

        // Update tracking information
        var (success, errorMessage) = await _orderStatusService.UpdateTrackingInformationAsync(
            subOrderId, TrackingNumber, CarrierName, TrackingUrl, userId);

        if (success)
        {
            TempData["SuccessMessage"] = "Tracking information updated successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMessage ?? "Failed to update tracking information.";
        }

        return RedirectToPage(new { subOrderId });
    }
}
