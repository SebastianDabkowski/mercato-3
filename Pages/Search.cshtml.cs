using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages;

public class SearchModel : PageModel
{
    private readonly IProductService _productService;
    private readonly ILogger<SearchModel> _logger;

    public SearchModel(
        IProductService productService,
        ILogger<SearchModel> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    public List<Product> Products { get; set; } = new();
    public int TotalProducts { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets the start index (1-based) for the current page.
    /// </summary>
    public int StartIndex => (CurrentPage - 1) * PageSize + 1;

    /// <summary>
    /// Gets the end index (1-based) for the current page.
    /// </summary>
    public int EndIndex => Math.Min(CurrentPage * PageSize, TotalProducts);

    public async Task<IActionResult> OnGetAsync(int page = 1)
    {
        if (page < 1)
        {
            page = 1;
        }

        CurrentPage = page;

        // If no query provided, show empty results
        if (string.IsNullOrWhiteSpace(Query))
        {
            return Page();
        }

        // Search for products
        var allProducts = await _productService.SearchProductsAsync(Query);
        TotalProducts = allProducts.Count;

        // Calculate pagination
        TotalPages = (int)Math.Ceiling(TotalProducts / (double)PageSize);

        // Ensure current page is within valid range
        if (CurrentPage > TotalPages && TotalPages > 0)
        {
            CurrentPage = TotalPages;
        }

        // Apply pagination
        Products = allProducts
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        return Page();
    }
}
