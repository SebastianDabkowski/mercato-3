using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Refunds;

/// <summary>
/// Page model for the admin refunds index page.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class IndexModel : PageModel
{
    private readonly IRefundService _refundService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IRefundService refundService,
        ILogger<IndexModel> logger)
    {
        _refundService = refundService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of refund transactions.
    /// </summary>
    public List<RefundTransaction> Refunds { get; set; } = new();

    /// <summary>
    /// Gets or sets the filter status.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? FilterStatus { get; set; }

    /// <summary>
    /// Gets or sets the filter order number.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? FilterOrderNumber { get; set; }

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
    /// Handles GET request to display refund transactions.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // Parse filter status if provided
            RefundStatus? statusFilter = null;
            if (!string.IsNullOrEmpty(FilterStatus) && Enum.TryParse<RefundStatus>(FilterStatus, out var status))
            {
                statusFilter = status;
            }

            // Get all refunds with optional status filter
            Refunds = await _refundService.GetAllRefundsAsync(statusFilter);

            // Filter by order number if specified (in-memory filter)
            if (!string.IsNullOrEmpty(FilterOrderNumber))
            {
                Refunds = Refunds.Where(r => 
                    r.Order.OrderNumber.Contains(FilterOrderNumber, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading refund transactions");
            ErrorMessage = "Error loading refund transactions.";
            return Page();
        }
    }

    /// <summary>
    /// Handles POST request to retry a failed refund.
    /// </summary>
    /// <param name="id">The refund transaction ID.</param>
    /// <returns>The action result.</returns>
    public async Task<IActionResult> OnPostRetryAsync(int id)
    {
        try
        {
            var refund = await _refundService.RetryFailedRefundAsync(id);

            if (refund.Status == RefundStatus.Completed)
            {
                SuccessMessage = $"Refund {refund.RefundNumber} has been successfully retried and completed.";
            }
            else
            {
                ErrorMessage = $"Refund retry failed: {refund.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying refund {RefundId}", id);
            ErrorMessage = $"Error retrying refund: {ex.Message}";
        }

        return RedirectToPage();
    }
}
