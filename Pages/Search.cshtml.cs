using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages;

public class SearchModel : PageModel
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<SearchModel> _logger;

    public SearchModel(
        IProductService productService,
        ICategoryService categoryService,
        ILogger<SearchModel> logger)
    {
        _productService = productService;
        _categoryService = categoryService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    [BindProperty(SupportsGet = true)]
    public List<int>? CategoryIds { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MinPrice { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MaxPrice { get; set; }

    [BindProperty(SupportsGet = true)]
    public List<ProductCondition>? Conditions { get; set; }

    [BindProperty(SupportsGet = true)]
    public List<int>? StoreIds { get; set; }

    [BindProperty(SupportsGet = true)]
    public ProductSortOption? SortBy { get; set; }

    public List<Product> Products { get; set; } = new();
    public int TotalProducts { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalPages { get; set; }
    public bool HasActiveFilters { get; set; }

    // Available filter options
    public List<CategoryTreeItem> AvailableCategories { get; set; } = new();
    public List<Store> AvailableStores { get; set; } = new();

    /// <summary>
    /// Gets the start index (1-based) for the current page.
    /// </summary>
    public int StartIndex => (CurrentPage - 1) * PageSize + 1;

    /// <summary>
    /// Gets the end index (1-based) for the current page.
    /// </summary>
    public int EndIndex => Math.Min(CurrentPage * PageSize, TotalProducts);

    /// <summary>
    /// Builds a URL for the specified page number with all current filters preserved.
    /// </summary>
    public string GetPageUrl(int pageNumber)
    {
        var queryParams = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(Query))
        {
            queryParams.Add($"query={Uri.EscapeDataString(Query)}");
        }
        
        queryParams.Add($"page={pageNumber}");
        
        if (SortBy.HasValue)
        {
            queryParams.Add($"sortby={SortBy.Value}");
        }
        
        if (MinPrice.HasValue)
        {
            queryParams.Add($"minprice={MinPrice.Value}");
        }
        
        if (MaxPrice.HasValue)
        {
            queryParams.Add($"maxprice={MaxPrice.Value}");
        }
        
        if (CategoryIds != null)
        {
            foreach (var catId in CategoryIds)
            {
                queryParams.Add($"CategoryIds={catId}");
            }
        }
        
        if (Conditions != null)
        {
            foreach (var condition in Conditions)
            {
                queryParams.Add($"Conditions={condition}");
            }
        }
        
        if (StoreIds != null)
        {
            foreach (var storeId in StoreIds)
            {
                queryParams.Add($"StoreIds={storeId}");
            }
        }
        
        return $"/Search?{string.Join("&", queryParams)}";
    }

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

        // Build filter
        var filter = new ProductFilter
        {
            CategoryIds = CategoryIds,
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            Conditions = Conditions,
            StoreIds = StoreIds,
            SortBy = SortBy
        };

        HasActiveFilters = filter.HasActiveFilters;

        // Search for products with filters
        var allProducts = await _productService.SearchProductsAsync(Query, filter);
        TotalProducts = allProducts.Count;

        // Load available filter options from search results
        if (allProducts.Count > 0)
        {
            // Get unique stores from results
            AvailableStores = allProducts
                .Where(p => p.Store != null)
                .Select(p => p.Store)
                .DistinctBy(s => s.Id)
                .OrderBy(s => s.StoreName)
                .ToList();

            // Get categories
            var categoryTree = await _categoryService.GetCategoryTreeAsync();
            AvailableCategories = FlattenCategoryTree(categoryTree).Where(c => c.IsActive).ToList();
        }

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

    private static List<CategoryTreeItem> FlattenCategoryTree(List<CategoryTreeItem> tree)
    {
        var result = new List<CategoryTreeItem>();
        foreach (var item in tree)
        {
            result.Add(item);
            if (item.Children.Count > 0)
            {
                result.AddRange(FlattenCategoryTree(item.Children));
            }
        }
        return result;
    }
}
