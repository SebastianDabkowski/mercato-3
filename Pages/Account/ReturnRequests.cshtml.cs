using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MercatoApp.Pages.Account;

/// <summary>
/// Page model for displaying all return/complaint requests for a buyer.
/// </summary>
[Authorize]
public class ReturnRequestsModel : PageModel
{
    private readonly IReturnRequestService _returnRequestService;
    private readonly ILogger<ReturnRequestsModel> _logger;

    public ReturnRequestsModel(
        IReturnRequestService returnRequestService,
        ILogger<ReturnRequestsModel> logger)
    {
        _returnRequestService = returnRequestService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of return requests for the buyer.
    /// </summary>
    public List<ReturnRequest> ReturnRequests { get; set; } = new();

    /// <summary>
    /// Gets or sets the unread message counts for each return request.
    /// Key is return request ID, value is unread count.
    /// </summary>
    public Dictionary<int, int> UnreadMessageCounts { get; set; } = new();

    /// <summary>
    /// Handles GET request to display all return requests for the logged-in buyer.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid");
            return RedirectToPage("/Account/Login");
        }

        // Get all return requests for the buyer
        ReturnRequests = await _returnRequestService.GetReturnRequestsByBuyerAsync(userId);

        // Get unread message counts for each return request
        foreach (var request in ReturnRequests)
        {
            var unreadCount = await _returnRequestService.GetUnreadMessageCountAsync(request.Id, userId, isSellerViewing: false);
            UnreadMessageCounts[request.Id] = unreadCount;
        }

        return Page();
    }
}
