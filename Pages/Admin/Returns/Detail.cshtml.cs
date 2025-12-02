using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
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
    private readonly IReturnRequestService _returnRequestService;

    public DetailModel(
        ApplicationDbContext context,
        ILogger<DetailModel> logger,
        IReturnRequestService returnRequestService)
    {
        _context = context;
        _logger = logger;
        _returnRequestService = returnRequestService;
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
    /// Gets or sets the admin actions taken on this case.
    /// </summary>
    public List<ReturnRequestAdminAction> AdminActions { get; set; } = new();

    /// <summary>
    /// Input model for escalation.
    /// </summary>
    [BindProperty]
    public EscalationInputModel? EscalationInput { get; set; }

    /// <summary>
    /// Input model for admin decision.
    /// </summary>
    [BindProperty]
    public AdminDecisionInputModel? DecisionInput { get; set; }

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
            .Include(rr => rr.EscalatedByUser)
            .FirstOrDefaultAsync(rr => rr.Id == id);

        if (ReturnRequest == null)
        {
            _logger.LogWarning("Return request {Id} not found", id);
            return NotFound();
        }

        // Get admin actions
        AdminActions = await _returnRequestService.GetAdminActionsAsync(id);

        // Try to find associated refund transaction
        if (ReturnRequest.Status == ReturnStatus.Completed || ReturnRequest.Status == ReturnStatus.Resolved)
        {
            RefundTransaction = await _context.RefundTransactions
                .FirstOrDefaultAsync(r => r.ReturnRequestId == id);
        }

        return Page();
    }

    /// <summary>
    /// Handles POST request to escalate a case.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostEscalateAsync()
    {
        if (EscalationInput == null || !ModelState.IsValid)
        {
            return RedirectToPage(new { id = EscalationInput?.ReturnRequestId });
        }

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        var (success, error) = await _returnRequestService.EscalateReturnCaseAsync(
            EscalationInput.ReturnRequestId,
            EscalationInput.EscalationReason,
            userId,
            EscalationInput.Notes);

        if (success)
        {
            TempData["SuccessMessage"] = "Case has been escalated for admin review.";
        }
        else
        {
            TempData["ErrorMessage"] = error ?? "Failed to escalate case.";
        }

        return RedirectToPage(new { id = EscalationInput.ReturnRequestId });
    }

    /// <summary>
    /// Handles POST request to record admin decision.
    /// </summary>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostRecordDecisionAsync()
    {
        if (DecisionInput == null || !ModelState.IsValid)
        {
            return RedirectToPage(new { id = DecisionInput?.ReturnRequestId });
        }

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        var (success, error, _) = await _returnRequestService.RecordAdminDecisionAsync(
            DecisionInput.ReturnRequestId,
            userId,
            DecisionInput.ActionType,
            DecisionInput.Notes,
            DecisionInput.NewStatus,
            DecisionInput.ResolutionType,
            DecisionInput.ResolutionAmount);

        if (success)
        {
            TempData["SuccessMessage"] = "Admin decision has been recorded.";
        }
        else
        {
            TempData["ErrorMessage"] = error ?? "Failed to record decision.";
        }

        return RedirectToPage(new { id = DecisionInput.ReturnRequestId });
    }

    /// <summary>
    /// Input model for escalation.
    /// </summary>
    public class EscalationInputModel
    {
        public int ReturnRequestId { get; set; }
        public EscalationReason EscalationReason { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Input model for admin decision.
    /// </summary>
    public class AdminDecisionInputModel
    {
        public int ReturnRequestId { get; set; }
        public AdminActionType ActionType { get; set; }
        public string Notes { get; set; } = string.Empty;
        public ReturnStatus? NewStatus { get; set; }
        public ResolutionType? ResolutionType { get; set; }
        public decimal? ResolutionAmount { get; set; }
    }
}
