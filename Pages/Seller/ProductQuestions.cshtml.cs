using MercatoApp.Authorization;
using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MercatoApp.Pages.Seller;

/// <summary>
/// Page model for seller to view and respond to product questions.
/// </summary>
[Authorize(Policy = PolicyNames.SellerOnly)]
public class ProductQuestionsModel : PageModel
{
    private readonly IProductQuestionService _questionService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductQuestionsModel> _logger;

    public ProductQuestionsModel(
        IProductQuestionService questionService,
        ApplicationDbContext context,
        ILogger<ProductQuestionsModel> logger)
    {
        _questionService = questionService;
        _context = context;
        _logger = logger;
    }

    public List<ProductQuestion> Questions { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int? productId = null)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Account/Login");
        }

        // Get seller's store using ApplicationDbContext
        var store = await _context.Stores
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (store == null)
        {
            ErrorMessage = "Store not found.";
            return Page();
        }

        // Load all unanswered questions for the store
        Questions = await _questionService.GetUnansweredQuestionsForStoreAsync(store.Id);

        // Filter by product if specified
        if (productId.HasValue)
        {
            Questions = Questions.Where(q => q.ProductId == productId.Value).ToList();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostReplyAsync(int questionId, string replyInput)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Account/Login");
        }

        if (string.IsNullOrWhiteSpace(replyInput))
        {
            ErrorMessage = "Please enter a reply.";
            return RedirectToPage();
        }

        try
        {
            await _questionService.ReplyToQuestionAsync(questionId, userId, replyInput, true);
            SuccessMessage = "Your reply has been sent to the buyer.";
            return RedirectToPage();
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "You are not authorized to reply to this question.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replying to question {QuestionId}", questionId);
            ErrorMessage = "Failed to send reply. Please try again.";
            return RedirectToPage();
        }
    }
}
