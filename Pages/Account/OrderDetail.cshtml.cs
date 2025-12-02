using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MercatoApp.Pages.Account;

/// <summary>
/// Page model for displaying detailed order information to a buyer.
/// </summary>
[Authorize]
public class OrderDetailModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderDetailModel> _logger;

    public OrderDetailModel(
        IOrderService orderService,
        ILogger<OrderDetailModel> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the order being viewed.
    /// </summary>
    public Order? Order { get; set; }

    /// <summary>
    /// Gets a value indicating whether the order has been refunded or partially refunded.
    /// </summary>
    public bool HasRefunds => Order != null && (Order.RefundedAmount > 0 || Order.SubOrders.Any(so => so.RefundedAmount > 0));

    /// <summary>
    /// Gets a value indicating whether the order has any cancelled or refunded sub-orders.
    /// </summary>
    public bool HasCancellations => Order != null && Order.SubOrders.Any(so => so.Status == OrderStatus.Cancelled || so.Status == OrderStatus.Refunded);

    /// <summary>
    /// Handles GET request to display order details.
    /// </summary>
    /// <param name="orderId">The order ID to display.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int orderId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid");
            return RedirectToPage("/Account/Login");
        }

        // Get order with authorization check
        Order = await _orderService.GetOrderByIdForBuyerAsync(orderId, userId);

        if (Order == null)
        {
            _logger.LogWarning("Order {OrderId} not found or user {UserId} not authorized to view it", orderId, userId);
            TempData["ErrorMessage"] = "Order not found or you don't have permission to view it.";
            return RedirectToPage("/Account/Orders");
        }

        return Page();
    }
}
