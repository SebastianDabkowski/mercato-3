using MercatoApp.Authorization;
using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Pages.Admin.ProductQuestions;

/// <summary>
/// Admin page for viewing and moderating product questions.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class IndexModel : PageModel
{
    private readonly IProductQuestionService _questionService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IProductQuestionService questionService,
        ApplicationDbContext context,
        ILogger<IndexModel> logger)
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

    public async Task<IActionResult> OnGetAsync()
    {
        // Load all questions (including hidden ones for admin)
        Questions = await _context.ProductQuestions
            .Include(q => q.Product)
                .ThenInclude(p => p.Store)
            .Include(q => q.Buyer)
            .Include(q => q.Replies)
            .OrderByDescending(q => q.AskedAt)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostHideAsync(int id)
    {
        try
        {
            await _questionService.HideQuestionAsync(id);
            SuccessMessage = "Question has been hidden.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hiding question {QuestionId}", id);
            ErrorMessage = "Failed to hide question.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostShowAsync(int id)
    {
        try
        {
            await _questionService.ShowQuestionAsync(id);
            SuccessMessage = "Question has been made visible.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing question {QuestionId}", id);
            ErrorMessage = "Failed to show question.";
        }

        return RedirectToPage();
    }
}
