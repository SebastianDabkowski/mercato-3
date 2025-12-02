using System.ComponentModel.DataAnnotations;
using MercatoApp.Authorization;
using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Pages.Admin.Refunds;

/// <summary>
/// Page model for processing refunds.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class ProcessRefundModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IRefundService _refundService;
    private readonly ILogger<ProcessRefundModel> _logger;

    public ProcessRefundModel(
        ApplicationDbContext context,
        IRefundService refundService,
        ILogger<ProcessRefundModel> logger)
    {
        _context = context;
        _refundService = refundService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the order ID to refund.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Order ID is required.")]
    public int OrderId { get; set; }

    /// <summary>
    /// Gets or sets the refund type (Full or Partial).
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Refund type is required.")]
    public string? RefundType { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order ID for partial refunds.
    /// </summary>
    [BindProperty]
    public int? SellerSubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the refund amount for partial refunds.
    /// </summary>
    [BindProperty]
    public decimal? RefundAmount { get; set; }

    /// <summary>
    /// Gets or sets the reason for the refund.
    /// </summary>
    [BindProperty]
    [Required(ErrorMessage = "Reason is required.")]
    [MaxLength(1000)]
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets optional notes about the refund.
    /// </summary>
    [BindProperty]
    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the order being refunded.
    /// </summary>
    public Order? Order { get; set; }

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
    /// Handles GET request to display the refund form.
    /// </summary>
    /// <param name="orderId">Optional order ID to pre-populate.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int? orderId)
    {
        if (orderId.HasValue)
        {
            OrderId = orderId.Value;
            Order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId.Value);
        }

        return Page();
    }

    /// <summary>
    /// Handles POST request to process a refund.
    /// </summary>
    /// <returns>The action result.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == OrderId);
            return Page();
        }

        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                ErrorMessage = "Unable to determine current user.";
                return RedirectToPage();
            }

            RefundTransaction refundTransaction;

            if (RefundType == "Full")
            {
                // Validate full refund eligibility
                var (isValid, validationError) = await _refundService.ValidateRefundEligibilityAsync(OrderId);
                if (!isValid)
                {
                    ErrorMessage = validationError;
                    Order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == OrderId);
                    return Page();
                }

                // Process full refund
                refundTransaction = await _refundService.ProcessFullRefundAsync(
                    OrderId,
                    Reason!,
                    userId,
                    Notes);

                SuccessMessage = $"Full refund {refundTransaction.RefundNumber} has been processed successfully.";
            }
            else if (RefundType == "Partial")
            {
                // Validate partial refund inputs
                if (!SellerSubOrderId.HasValue)
                {
                    ErrorMessage = "Seller Sub-Order ID is required for partial refunds.";
                    Order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == OrderId);
                    return Page();
                }

                if (!RefundAmount.HasValue || RefundAmount.Value <= 0)
                {
                    ErrorMessage = "Refund amount must be greater than zero.";
                    Order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == OrderId);
                    return Page();
                }

                // Validate partial refund eligibility
                var (isValid, validationError) = await _refundService.ValidatePartialRefundEligibilityAsync(
                    SellerSubOrderId.Value,
                    RefundAmount.Value);

                if (!isValid)
                {
                    ErrorMessage = validationError;
                    Order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == OrderId);
                    return Page();
                }

                // Process partial refund
                refundTransaction = await _refundService.ProcessPartialRefundAsync(
                    OrderId,
                    SellerSubOrderId.Value,
                    RefundAmount.Value,
                    Reason!,
                    userId,
                    Notes);

                SuccessMessage = $"Partial refund {refundTransaction.RefundNumber} has been processed successfully.";
            }
            else
            {
                ErrorMessage = "Invalid refund type.";
                Order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == OrderId);
                return Page();
            }

            if (refundTransaction.Status == RefundStatus.Failed)
            {
                ErrorMessage = $"Refund processing failed: {refundTransaction.ErrorMessage}";
                return RedirectToPage();
            }

            return RedirectToPage("/Admin/Refunds/Details", new { id = refundTransaction.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for order {OrderId}", OrderId);
            ErrorMessage = $"Error processing refund: {ex.Message}";
            Order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == OrderId);
            return Page();
        }
    }
}
