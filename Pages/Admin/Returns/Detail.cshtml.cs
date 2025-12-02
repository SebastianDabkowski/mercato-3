using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Pages.Admin.Returns;

/// <summary>
/// Page model for admin view of a specific return request with full messaging thread.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class DetailModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DetailModel> _logger;

    public DetailModel(
        ApplicationDbContext context,
        ILogger<DetailModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the return request.
    /// </summary>
    public ReturnRequest? ReturnRequest { get; set; }

    /// <summary>
    /// Gets or sets the associated refund transaction, if any.
    /// </summary>
    public RefundTransaction? RefundTransaction { get; set; }

    /// <summary>
    /// Handles GET request to display return request details.
    /// </summary>
    /// <param name="id">The return request ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        // Get the return request with all related data
        ReturnRequest = await _context.ReturnRequests
            .Include(rr => rr.SubOrder)
                .ThenInclude(so => so.Store)
            .Include(rr => rr.SubOrder)
                .ThenInclude(so => so.ParentOrder)
            .Include(rr => rr.SubOrder)
                .ThenInclude(so => so.Items)
            .Include(rr => rr.Buyer)
            .Include(rr => rr.Items)
                .ThenInclude(ri => ri.OrderItem)
            .Include(rr => rr.Messages)
                .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(rr => rr.Id == id);

        if (ReturnRequest == null)
        {
            _logger.LogWarning("Return request {Id} not found", id);
            return NotFound();
        }

        // Try to find associated refund transaction
        if (ReturnRequest.Status == ReturnStatus.Completed)
        {
            RefundTransaction = await _context.RefundTransactions
                .FirstOrDefaultAsync(r => r.ReturnRequestId == id);
        }

        return Page();
    }
}
