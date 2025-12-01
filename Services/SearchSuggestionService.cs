using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Represents a search suggestion item.
/// </summary>
public class SearchSuggestion
{
    /// <summary>
    /// Type of the suggestion (query, category, product).
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Display text for the suggestion.
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// URL to navigate to when suggestion is clicked.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Value to populate in search input (for query suggestions).
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Category ID (for category suggestions).
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Product ID (for product suggestions).
    /// </summary>
    public int? ProductId { get; set; }
}

/// <summary>
/// Configuration for search suggestions.
/// </summary>
public class SearchSuggestionSettings
{
    /// <summary>
    /// Minimum number of characters required before showing suggestions.
    /// </summary>
    public int MinimumCharacters { get; set; } = 2;

    /// <summary>
    /// Maximum number of suggestions to return.
    /// </summary>
    public int MaxSuggestions { get; set; } = 10;

    /// <summary>
    /// Maximum number of category suggestions.
    /// </summary>
    public int MaxCategorySuggestions { get; set; } = 3;

    /// <summary>
    /// Maximum number of product suggestions.
    /// </summary>
    public int MaxProductSuggestions { get; set; } = 5;
}

/// <summary>
/// Interface for search suggestion service.
/// </summary>
public interface ISearchSuggestionService
{
    /// <summary>
    /// Gets search suggestions based on the provided query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <returns>A list of search suggestions.</returns>
    Task<List<SearchSuggestion>> GetSuggestionsAsync(string query);
}

/// <summary>
/// Service for providing search suggestions.
/// </summary>
public class SearchSuggestionService : ISearchSuggestionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SearchSuggestionService> _logger;
    private readonly SearchSuggestionSettings _settings;

    public SearchSuggestionService(
        ApplicationDbContext context,
        ILogger<SearchSuggestionService> logger)
    {
        _context = context;
        _logger = logger;
        _settings = new SearchSuggestionSettings();
    }

    /// <inheritdoc />
    public async Task<List<SearchSuggestion>> GetSuggestionsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < _settings.MinimumCharacters)
        {
            return new List<SearchSuggestion>();
        }

        var suggestions = new List<SearchSuggestion>();
        var sanitizedQuery = query.Trim();
        var lowerQuery = sanitizedQuery.ToLower();

        try
        {
            // Get category suggestions
            var categories = await GetCategorySuggestionsAsync(lowerQuery);
            suggestions.AddRange(categories);

            // Get product suggestions
            var products = await GetProductSuggestionsAsync(lowerQuery);
            suggestions.AddRange(products);

            _logger.LogInformation(
                "Generated {Count} suggestions for query '{Query}'",
                suggestions.Count,
                sanitizedQuery);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating suggestions for query '{Query}'", sanitizedQuery);
        }

        return suggestions.Take(_settings.MaxSuggestions).ToList();
    }

    /// <summary>
    /// Gets category suggestions matching the query.
    /// </summary>
    private async Task<List<SearchSuggestion>> GetCategorySuggestionsAsync(string lowerQuery)
    {
        var matchingCategories = await _context.Categories
            .Where(c => c.IsActive && EF.Functions.Like(c.Name, $"%{lowerQuery}%"))
            .OrderBy(c => c.Name)
            .Take(_settings.MaxCategorySuggestions)
            .Select(c => new SearchSuggestion
            {
                Type = "category",
                Text = c.Name,
                Url = $"/Category?id={c.Id}",
                CategoryId = c.Id
            })
            .ToListAsync();

        return matchingCategories;
    }

    /// <summary>
    /// Gets product suggestions matching the query.
    /// </summary>
    private async Task<List<SearchSuggestion>> GetProductSuggestionsAsync(string lowerQuery)
    {
        var matchingProducts = await _context.Products
            .Where(p => p.Status == ProductStatus.Active &&
                       EF.Functions.Like(p.Title, $"%{lowerQuery}%"))
            .OrderByDescending(p => p.CreatedAt)
            .Take(_settings.MaxProductSuggestions)
            .Select(p => new SearchSuggestion
            {
                Type = "product",
                Text = p.Title,
                Url = $"/Product?id={p.Id}",
                ProductId = p.Id
            })
            .ToListAsync();

        return matchingProducts;
    }
}
