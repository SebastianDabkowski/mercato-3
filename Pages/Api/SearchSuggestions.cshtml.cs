using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Api;

/// <summary>
/// API endpoint for search suggestions.
/// </summary>
public class SearchSuggestionsModel : PageModel
{
    private readonly ISearchSuggestionService _searchSuggestionService;
    private readonly ILogger<SearchSuggestionsModel> _logger;

    public SearchSuggestionsModel(
        ISearchSuggestionService searchSuggestionService,
        ILogger<SearchSuggestionsModel> logger)
    {
        _searchSuggestionService = searchSuggestionService;
        _logger = logger;
    }

    /// <summary>
    /// Handles GET requests for search suggestions.
    /// </summary>
    /// <param name="q">The search query.</param>
    /// <returns>JSON array of search suggestions.</returns>
    public async Task<IActionResult> OnGetAsync([FromQuery] string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return new JsonResult(new List<SearchSuggestion>());
        }

        try
        {
            var suggestions = await _searchSuggestionService.GetSuggestionsAsync(q);
            return new JsonResult(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for query '{Query}'", q);
            return new JsonResult(new List<SearchSuggestion>())
            {
                StatusCode = 500
            };
        }
    }
}
