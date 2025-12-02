using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MercatoApp.Pages.Account;

/// <summary>
/// Page model for displaying detailed information about a return/complaint request.
/// </summary>
[Authorize]
public class ReturnRequestDetailModel : PageModel
{
    private readonly IReturnRequestService _returnRequestService;
    private readonly IRefundService _refundService;
    private readonly ILogger<ReturnRequestDetailModel> _logger;

    public ReturnRequestDetailModel(
        IReturnRequestService returnRequestService,
        IRefundService refundService,
        ILogger<ReturnRequestDetailModel> logger)
    {
        _returnRequestService = returnRequestService;
        _refundService = refundService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the return request details.
    /// </summary>
    public ReturnRequest ReturnRequest { get; set; } = null!;

    /// <summary>
    /// Gets or sets the associated refund transaction, if any.
    /// </summary>
    public RefundTransaction? RefundTransaction { get; set; }

    /// <summary>
    /// Gets or sets the new message content input.
    /// </summary>
    [BindProperty]
    public string NewMessageContent { get; set; } = string.Empty;

    /// <summary>
    /// Handles GET request to display return request details.
    /// </summary>
    /// <param name="id">The return request ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid");
            return RedirectToPage("/Account/Login");
        }

        // Get the return request with all related data
        var returnRequest = await _returnRequestService.GetReturnRequestByIdAsync(id);

        if (returnRequest == null)
        {
            _logger.LogWarning("Return request {Id} not found", id);
            return NotFound();
        }

        // Authorization check: ensure the buyer owns this return request
        if (returnRequest.BuyerId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to access return request {Id} belonging to user {BuyerId}", 
                userId, id, returnRequest.BuyerId);
            return Forbid();
        }

        ReturnRequest = returnRequest;

        // Try to find associated refund transaction
        if (returnRequest.Status == ReturnStatus.Completed)
        {
            var refunds = await _refundService.GetRefundsByOrderAsync(returnRequest.SubOrder.ParentOrder.Id);
            RefundTransaction = refunds.FirstOrDefault(r => r.ReturnRequestId == id);
        }

        // Mark messages as read for the buyer
        await _returnRequestService.MarkMessagesAsReadAsync(id, userId, isSellerViewing: false);

        return Page();
    }

    /// <summary>
    /// Handles POST request to add a message to the return request.
    /// </summary>
    /// <param name="id">The return request ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostAddMessageAsync(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid");
            return RedirectToPage("/Account/Login");
        }

        // Validate message content
        if (string.IsNullOrWhiteSpace(NewMessageContent))
        {
            TempData["ErrorMessage"] = "Message cannot be empty.";
            return RedirectToPage(new { id });
        }

        if (NewMessageContent.Length > 2000)
        {
            TempData["ErrorMessage"] = "Message cannot exceed 2000 characters.";
            return RedirectToPage(new { id });
        }

        // Add the message
        var message = await _returnRequestService.AddMessageAsync(id, userId, NewMessageContent, isFromSeller: false);

        if (message == null)
        {
            TempData["ErrorMessage"] = "Failed to send message. You may not have permission to message this case.";
            return RedirectToPage(new { id });
        }

        TempData["SuccessMessage"] = "Message sent successfully.";
        return RedirectToPage(new { id });
    }
}
