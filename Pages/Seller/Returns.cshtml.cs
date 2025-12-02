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
/// Page model for displaying all return/complaint requests for a seller's store.
/// </summary>
[Authorize(Policy = "SellerOnly")]
public class ReturnsModel : PageModel
{
    private readonly IReturnRequestService _returnRequestService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReturnsModel> _logger;

    public ReturnsModel(
        IReturnRequestService returnRequestService,
        ApplicationDbContext context,
        ILogger<ReturnsModel> logger)
    {
        _returnRequestService = returnRequestService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the current store.
    /// </summary>
    public Store? CurrentStore { get; set; }

    /// <summary>
    /// Gets or sets the list of return requests.
    /// </summary>
    public List<ReturnRequest> ReturnRequests { get; set; } = new();

    /// <summary>
    /// Gets or sets the status filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public ReturnStatus? StatusFilter { get; set; }

    /// <summary>
    /// Gets or sets the type filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public ReturnRequestType? TypeFilter { get; set; }

    /// <summary>
    /// Gets or sets the buyer email filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? BuyerEmail { get; set; }

    /// <summary>
    /// Gets or sets the unread message counts for each return request.
    /// Key is return request ID, value is unread count.
    /// </summary>
    public Dictionary<int, int> UnreadMessageCounts { get; set; } = new();

    /// <summary>
    /// Handles GET request to display all return requests for the seller's store.
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

        // Get the seller's store
        CurrentStore = await _context.Stores
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (CurrentStore == null)
        {
            TempData["ErrorMessage"] = "Store not found.";
            return RedirectToPage("/Index");
        }

        // Get all return requests for this store
        ReturnRequests = await _returnRequestService.GetReturnRequestsByStoreAsync(CurrentStore.Id);

        // Apply filters
        if (StatusFilter.HasValue)
        {
            ReturnRequests = ReturnRequests.Where(r => r.Status == StatusFilter.Value).ToList();
        }

        if (TypeFilter.HasValue)
        {
            ReturnRequests = ReturnRequests.Where(r => r.RequestType == TypeFilter.Value).ToList();
        }

        if (!string.IsNullOrWhiteSpace(BuyerEmail))
        {
            ReturnRequests = ReturnRequests.Where(r => 
                r.Buyer.Email.Contains(BuyerEmail, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Get unread message counts for each return request
        foreach (var request in ReturnRequests)
        {
            var unreadCount = await _returnRequestService.GetUnreadMessageCountAsync(request.Id, userId, isSellerViewing: true);
            UnreadMessageCounts[request.Id] = unreadCount;
        }

        return Page();
    }
}
