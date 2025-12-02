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
    private readonly IReturnRequestService _returnRequestService;
    private readonly ILogger<OrderDetailModel> _logger;

    public OrderDetailModel(
        IOrderService orderService,
        IReturnRequestService returnRequestService,
        ILogger<OrderDetailModel> logger)
    {
        _orderService = orderService;
        _returnRequestService = returnRequestService;
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
    /// Gets or sets the return requests for this order's sub-orders.
    /// </summary>
    public Dictionary<int, List<ReturnRequest>> ReturnRequestsBySubOrder { get; set; } = new Dictionary<int, List<ReturnRequest>>();

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

        // Load return requests for all sub-orders
        foreach (var subOrder in Order.SubOrders)
        {
            var returns = await _returnRequestService.GetReturnRequestsBySubOrderAsync(subOrder.Id);
            if (returns.Any())
            {
                ReturnRequestsBySubOrder[subOrder.Id] = returns;
            }
        }

        return Page();
    }

    /// <summary>
    /// Handles POST request to initiate a return for a sub-order.
    /// </summary>
    /// <param name="subOrderId">The sub-order ID to return.</param>
    /// <param name="orderId">The parent order ID for redirect.</param>
    /// <param name="requestType">The type of request (return or complaint).</param>
    /// <param name="reason">The return reason.</param>
    /// <param name="description">Optional description from buyer.</param>
    /// <param name="isFullReturn">Whether to return all items.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostInitiateReturnAsync(
        int subOrderId,
        int orderId,
        ReturnRequestType requestType,
        ReturnReason reason, 
        string? description, 
        bool isFullReturn = true)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid");
            return RedirectToPage("/Account/Login");
        }

        try
        {
            // Create the return request
            var returnRequest = await _returnRequestService.CreateReturnRequestAsync(
                subOrderId,
                userId,
                requestType,
                reason,
                description,
                isFullReturn);

            var requestTypeLabel = requestType == ReturnRequestType.Complaint ? "Complaint" : "Return";
            TempData["SuccessMessage"] = $"{requestTypeLabel} request {returnRequest.ReturnNumber} has been submitted successfully. The seller will review it shortly.";
            
            // Redirect back to order detail to show the return status
            return RedirectToPage(new { orderId });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create return request for sub-order {SubOrderId}", subOrderId);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage(new { orderId });
        }
    }
}
