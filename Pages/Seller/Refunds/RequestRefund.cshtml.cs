using System.ComponentModel.DataAnnotations;
using MercatoApp.Authorization;
using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Pages.Seller.Refunds;

/// <summary>
/// Page model for sellers to request refunds for their sub-orders.
/// </summary>
[Authorize(Policy = PolicyNames.SellerOnly)]
public class RequestRefundModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IRefundService _refundService;
    private readonly ILogger<RequestRefundModel> _logger;

    public RequestRefundModel(
        ApplicationDbContext context,
        IRefundService refundService,
        ILogger<RequestRefundModel> logger)
    {
        _context = context;
        _refundService = refundService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the seller sub-order ID.
    /// </summary>
    [BindProperty]
    public int SubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the refund amount.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Refund amount is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Refund amount must be greater than zero.")]
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Gets or sets the reason for the refund.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Reason is required.")]
    [MaxLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters.")]
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order.
    /// </summary>
    public SellerSubOrder? SubOrder { get; set; }

    /// <summary>
    /// Gets or sets the error message to display.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the success message to display.
    /// </summary>
    [TempData]
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Handles GET request to display the refund request form.
    /// </summary>
    /// <param name="subOrderId">The seller sub-order ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int subOrderId)
    {
        SubOrderId = subOrderId;

        // Get current user's store
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            ErrorMessage = "Unable to determine current user.";
            return RedirectToPage("/Seller/Orders");
        }

        var store = await _context.Stores.FirstOrDefaultAsync(s => s.UserId == userId);
        if (store == null)
        {
            ErrorMessage = "Store not found.";
            return RedirectToPage("/Seller/Orders");
        }

        // Load sub-order and verify it belongs to this seller
        SubOrder = await _context.SellerSubOrders
            .Include(so => so.ParentOrder)
            .FirstOrDefaultAsync(so => so.Id == subOrderId && so.StoreId == store.Id);

        if (SubOrder == null)
        {
            ErrorMessage = "Sub-order not found or you don't have permission to access it.";
            return RedirectToPage("/Seller/Orders");
        }

        // Validate that refund is possible
        var (isValid, validationError) = await ValidateRefundEligibilityForSeller(SubOrder);
        if (!isValid)
        {
            ErrorMessage = validationError;
        }

        return Page();
    }

    /// <summary>
    /// Handles POST request to submit a refund request.
    /// </summary>
    /// <returns>The action result.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            // Reload sub-order for display
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                var store = await _context.Stores.FirstOrDefaultAsync(s => s.UserId == userId);
                if (store != null)
                {
                    SubOrder = await _context.SellerSubOrders
                        .Include(so => so.ParentOrder)
                        .FirstOrDefaultAsync(so => so.Id == SubOrderId && so.StoreId == store.Id);
                }
            }
            return Page();
        }

        try
        {
            // Get current user
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                ErrorMessage = "Unable to determine current user.";
                return RedirectToPage("/Seller/Orders");
            }

            // Get seller's store
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.UserId == userId);
            if (store == null)
            {
                ErrorMessage = "Store not found.";
                return RedirectToPage("/Seller/Orders");
            }

            // Verify sub-order belongs to this seller
            SubOrder = await _context.SellerSubOrders
                .Include(so => so.ParentOrder)
                .FirstOrDefaultAsync(so => so.Id == SubOrderId && so.StoreId == store.Id);

            if (SubOrder == null)
            {
                ErrorMessage = "Sub-order not found or you don't have permission to access it.";
                return RedirectToPage("/Seller/Orders");
            }

            // Validate refund eligibility
            var (isValid, validationError) = await ValidateRefundEligibilityForSeller(SubOrder);
            if (!isValid)
            {
                ErrorMessage = validationError;
                return Page();
            }

            // Additional validation for refund amount
            var (isAmountValid, amountError) = await _refundService.ValidatePartialRefundEligibilityAsync(
                SubOrderId, RefundAmount);

            if (!isAmountValid)
            {
                ErrorMessage = amountError;
                return Page();
            }

            // Process the partial refund request
            var refundTransaction = await _refundService.ProcessPartialRefundAsync(
                SubOrder.ParentOrderId,
                SubOrderId,
                RefundAmount,
                Reason!,
                userId,
                "Seller-initiated refund request");

            if (refundTransaction.Status == RefundStatus.Completed)
            {
                SuccessMessage = $"Refund request {refundTransaction.RefundNumber} has been processed successfully.";
                return RedirectToPage("/Seller/OrderDetails", new { id = SubOrderId });
            }
            else if (refundTransaction.Status == RefundStatus.Failed)
            {
                ErrorMessage = $"Refund request failed: {refundTransaction.ErrorMessage}";
                return Page();
            }
            else
            {
                SuccessMessage = $"Refund request {refundTransaction.RefundNumber} has been submitted and is pending processing.";
                return RedirectToPage("/Seller/OrderDetails", new { id = SubOrderId });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing seller refund request for sub-order {SubOrderId}", SubOrderId);
            ErrorMessage = $"Error processing refund request: {ex.Message}";
            
            // Reload sub-order for display
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                var store = await _context.Stores.FirstOrDefaultAsync(s => s.UserId == userId);
                if (store != null)
                {
                    SubOrder = await _context.SellerSubOrders
                        .Include(so => so.ParentOrder)
                        .FirstOrDefaultAsync(so => so.Id == SubOrderId && so.StoreId == store.Id);
                }
            }
            
            return Page();
        }
    }

    /// <summary>
    /// Validates if a seller can request a refund for the given sub-order.
    /// Enforces business rules for seller-initiated refunds.
    /// </summary>
    /// <param name="subOrder">The seller sub-order.</param>
    /// <returns>A tuple indicating if refund is valid and an error message if not.</returns>
    private async Task<(bool IsValid, string? ErrorMessage)> ValidateRefundEligibilityForSeller(SellerSubOrder subOrder)
    {
        // Check if sub-order has been delivered - sellers cannot refund delivered orders
        if (subOrder.Status == OrderStatus.Delivered)
        {
            return (false, "Cannot request refund for delivered orders. Please contact support if you need assistance.");
        }

        // Check if sub-order is cancelled - cannot refund cancelled orders
        if (subOrder.Status == OrderStatus.Cancelled)
        {
            return (false, "Cannot request refund for cancelled orders.");
        }

        // Check if parent order has a completed payment
        var hasCompletedPayment = await _context.PaymentTransactions
            .Where(pt => pt.OrderId == subOrder.ParentOrderId)
            .AnyAsync(pt => pt.Status == PaymentStatus.Completed || pt.Status == PaymentStatus.Authorized);

        if (!hasCompletedPayment)
        {
            return (false, "Cannot request refund - order does not have a completed payment.");
        }

        // Check available refund amount
        var availableAmount = subOrder.TotalAmount - subOrder.RefundedAmount;
        if (availableAmount <= 0)
        {
            return (false, "No amount available to refund. Sub-order has been fully refunded.");
        }

        // Check escrow status
        var escrowTransaction = await _context.EscrowTransactions
            .FirstOrDefaultAsync(et => et.SellerSubOrderId == subOrder.Id);

        if (escrowTransaction != null && escrowTransaction.Status == EscrowStatus.Released)
        {
            return (false, "Cannot request refund - escrow has already been released. Please contact support.");
        }

        return (true, null);
    }
}
