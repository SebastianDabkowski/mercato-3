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
public class PartialFulfillmentModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IOrderItemFulfillmentService _itemFulfillmentService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PartialFulfillmentModel> _logger;

    public PartialFulfillmentModel(
        IOrderService orderService,
        IOrderItemFulfillmentService itemFulfillmentService,
        ApplicationDbContext context,
        ILogger<PartialFulfillmentModel> logger)
    {
        _orderService = orderService;
        _itemFulfillmentService = itemFulfillmentService;
        _context = context;
        _logger = logger;
    }

    public SellerSubOrder? SubOrder { get; set; }
    public Store? CurrentStore { get; set; }
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();

    [BindProperty]
    public Dictionary<int, int> ItemQuantitiesToShip { get; set; } = new Dictionary<int, int>();

    [BindProperty]
    public Dictionary<int, int> ItemQuantitiesToCancel { get; set; } = new Dictionary<int, int>();

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

        // Get the sub-order with authorization check
        SubOrder = await _orderService.GetSubOrderByIdForSellerAsync(subOrderId, userId);

        if (SubOrder == null)
        {
            TempData["ErrorMessage"] = "Order not found or you don't have permission to view it.";
            return RedirectToPage("/Seller/Orders");
        }

        // Get items with fulfillment status
        Items = await _itemFulfillmentService.GetSubOrderItemsWithStatusAsync(subOrderId);

        // Validate if partial fulfillment is allowed
        var (isAllowed, errorMessage) = await _itemFulfillmentService.ValidateItemFulfillmentAsync(subOrderId);
        if (!isAllowed)
        {
            TempData["ErrorMessage"] = errorMessage;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostShipItemsAsync(int subOrderId)
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

        // Get the sub-order with authorization check
        SubOrder = await _orderService.GetSubOrderByIdForSellerAsync(subOrderId, userId);

        if (SubOrder == null)
        {
            TempData["ErrorMessage"] = "Order not found or you don't have permission to access it.";
            return RedirectToPage("/Seller/Orders");
        }

        var successCount = 0;
        var errorMessages = new List<string>();

        // Process each item
        foreach (var kvp in ItemQuantitiesToShip)
        {
            var itemId = kvp.Key;
            var quantity = kvp.Value;

            if (quantity > 0)
            {
                var (success, errorMessage) = await _itemFulfillmentService.ShipItemQuantityAsync(
                    itemId, quantity, userId);

                if (success)
                {
                    successCount++;
                }
                else if (errorMessage != null)
                {
                    errorMessages.Add($"Item #{itemId}: {errorMessage}");
                }
            }
        }

        if (successCount > 0)
        {
            TempData["SuccessMessage"] = $"Successfully shipped {successCount} item(s).";
        }

        if (errorMessages.Any())
        {
            TempData["ErrorMessage"] = string.Join("<br/>", errorMessages);
        }

        return RedirectToPage(new { subOrderId });
    }

    public async Task<IActionResult> OnPostCancelItemsAsync(int subOrderId)
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

        // Get the sub-order with authorization check
        SubOrder = await _orderService.GetSubOrderByIdForSellerAsync(subOrderId, userId);

        if (SubOrder == null)
        {
            TempData["ErrorMessage"] = "Order not found or you don't have permission to access it.";
            return RedirectToPage("/Seller/Orders");
        }

        var successCount = 0;
        var totalRefundAmount = 0m;
        var errorMessages = new List<string>();

        // Process each item
        foreach (var kvp in ItemQuantitiesToCancel)
        {
            var itemId = kvp.Key;
            var quantity = kvp.Value;

            if (quantity > 0)
            {
                var (success, errorMessage, refundAmount) = await _itemFulfillmentService.CancelItemQuantityAsync(
                    itemId, quantity, userId);

                if (success)
                {
                    successCount++;
                    totalRefundAmount += refundAmount;
                }
                else if (errorMessage != null)
                {
                    errorMessages.Add($"Item #{itemId}: {errorMessage}");
                }
            }
        }

        if (successCount > 0)
        {
            TempData["SuccessMessage"] = $"Successfully cancelled {successCount} item(s). Refund amount: {totalRefundAmount:C}";
        }

        if (errorMessages.Any())
        {
            TempData["ErrorMessage"] = string.Join("<br/>", errorMessages);
        }

        return RedirectToPage(new { subOrderId });
    }
}
