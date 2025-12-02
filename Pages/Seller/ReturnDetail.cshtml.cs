using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MercatoApp.Pages.Seller;

/// <summary>
/// Page model for viewing and acting on a specific return/complaint request.
/// </summary>
[Authorize(Policy = "SellerOnly")]
public class ReturnDetailModel : PageModel
{
    private readonly IReturnRequestService _returnRequestService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReturnDetailModel> _logger;

    public ReturnDetailModel(
        IReturnRequestService returnRequestService,
        ApplicationDbContext context,
        ILogger<ReturnDetailModel> logger)
    {
        _returnRequestService = returnRequestService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the return request.
    /// </summary>
    public ReturnRequest? ReturnRequest { get; set; }

    /// <summary>
    /// Gets or sets the current store.
    /// </summary>
    public Store? CurrentStore { get; set; }

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

        // Get the seller's store
        CurrentStore = await _context.Stores
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (CurrentStore == null)
        {
            TempData["ErrorMessage"] = "Store not found.";
            return RedirectToPage("/Index");
        }

        // Get the return request
        ReturnRequest = await _returnRequestService.GetReturnRequestByIdAsync(id);

        if (ReturnRequest == null)
        {
            TempData["ErrorMessage"] = "Return request not found.";
            return RedirectToPage("/Seller/Returns");
        }

        // Verify the return request belongs to this seller's store
        if (ReturnRequest.SubOrder.StoreId != CurrentStore.Id)
        {
            _logger.LogWarning("Store {StoreId} attempted to access return request {ReturnRequestId} belonging to store {ActualStoreId}",
                CurrentStore.Id, id, ReturnRequest.SubOrder.StoreId);
            TempData["ErrorMessage"] = "You are not authorized to view this return request.";
            return RedirectToPage("/Seller/Returns");
        }

        return Page();
    }

    /// <summary>
    /// Handles POST request to approve a return request.
    /// </summary>
    /// <param name="returnRequestId">The return request ID.</param>
    /// <param name="sellerNotes">Optional seller notes.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostApproveAsync(int returnRequestId, string? sellerNotes)
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

        // Validate seller notes length if provided
        if (!string.IsNullOrWhiteSpace(sellerNotes) && sellerNotes.Length > 1000)
        {
            TempData["ErrorMessage"] = "Seller notes cannot exceed 1000 characters.";
            return RedirectToPage("/Seller/ReturnDetail", new { id = returnRequestId });
        }

        // Approve the return request
        var success = await _returnRequestService.ApproveReturnRequestAsync(returnRequestId, CurrentStore.Id, sellerNotes);

        if (success)
        {
            TempData["SuccessMessage"] = "Return request approved successfully. The buyer has been notified.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to approve return request. Please try again.";
        }

        return RedirectToPage("/Seller/ReturnDetail", new { id = returnRequestId });
    }

    /// <summary>
    /// Handles POST request to reject a return request.
    /// </summary>
    /// <param name="returnRequestId">The return request ID.</param>
    /// <param name="sellerNotes">Required seller notes explaining rejection.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostRejectAsync(int returnRequestId, string sellerNotes)
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

        // Validate seller notes
        if (string.IsNullOrWhiteSpace(sellerNotes))
        {
            TempData["ErrorMessage"] = "You must provide a reason for rejecting the return request.";
            return RedirectToPage("/Seller/ReturnDetail", new { id = returnRequestId });
        }

        if (sellerNotes.Length > 1000)
        {
            TempData["ErrorMessage"] = "Seller notes cannot exceed 1000 characters.";
            return RedirectToPage("/Seller/ReturnDetail", new { id = returnRequestId });
        }

        // Reject the return request
        try
        {
            var success = await _returnRequestService.RejectReturnRequestAsync(returnRequestId, CurrentStore.Id, sellerNotes);

            if (success)
            {
                TempData["SuccessMessage"] = "Return request rejected. The buyer has been notified.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to reject return request. Please try again.";
            }
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage("/Seller/ReturnDetail", new { id = returnRequestId });
    }
}
