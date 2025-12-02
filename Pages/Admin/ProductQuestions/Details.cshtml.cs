using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.ProductQuestions;

/// <summary>
/// Admin page for viewing product question details.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class DetailsModel : PageModel
{
    private readonly IProductQuestionService _questionService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IProductQuestionService questionService,
        ILogger<DetailsModel> logger)
    {
        _questionService = questionService;
        _logger = logger;
    }

    public ProductQuestion? Question { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Question = await _questionService.GetQuestionByIdAsync(id);
        
        if (Question == null)
        {
            return NotFound();
        }

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

        return RedirectToPage(new { id });
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

        return RedirectToPage(new { id });
    }
}
