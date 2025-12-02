using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Refunds;

/// <summary>
/// Page model for viewing refund transaction details.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class DetailsModel : PageModel
{
    private readonly IRefundService _refundService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IRefundService refundService,
        ILogger<DetailsModel> logger)
    {
        _refundService = refundService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the refund transaction.
    /// </summary>
    public RefundTransaction? Refund { get; set; }

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
    /// Handles GET request to display refund details.
    /// </summary>
    /// <param name="id">The refund transaction ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        Refund = await _refundService.GetRefundByIdAsync(id);

        if (Refund == null)
        {
            return NotFound();
        }

        return Page();
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

        return RedirectToPage(new { id });
    }
}
