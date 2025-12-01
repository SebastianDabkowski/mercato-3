using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages;

public class CategoryModel : PageModel
{
    private readonly ICategoryService _categoryService;
    private readonly IProductService _productService;
    private readonly ILogger<CategoryModel> _logger;

    public CategoryModel(
        ICategoryService categoryService,
        IProductService productService,
        ILogger<CategoryModel> logger)
    {
        _categoryService = categoryService;
        _productService = productService;
        _logger = logger;
    }

    public Category? Category { get; set; }
    public List<CategoryTreeItem> Subcategories { get; set; } = new();
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

    public async Task<IActionResult> OnGetAsync(int id, int page = 1)
    {
        if (page < 1)
        {
            page = 1;
        }

        CurrentPage = page;

        // Load the category
        Category = await _categoryService.GetCategoryByIdAsync(id);

        if (Category == null)
        {
            return NotFound();
        }

        // Only show active categories to buyers
        if (!Category.IsActive)
        {
            return NotFound();
        }

        // Load subcategories
        var allCategories = await _categoryService.GetCategoryTreeAsync();
        var categoryTreeItem = FindCategoryInTree(allCategories, id);
        if (categoryTreeItem != null)
        {
            Subcategories = categoryTreeItem.Children.Where(c => c.IsActive).ToList();
        }

        // Get products for this category (including subcategories)
        var categoryIds = new List<int> { id };
        var descendantIds = await _categoryService.GetDescendantCategoryIdsAsync(id);
        categoryIds.AddRange(descendantIds);

        var allProducts = await _productService.GetProductsByCategoryIdsAsync(categoryIds);
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

    private CategoryTreeItem? FindCategoryInTree(List<CategoryTreeItem> tree, int categoryId)
    {
        foreach (var item in tree)
        {
            if (item.Id == categoryId)
            {
                return item;
            }

            var found = FindCategoryInTree(item.Children, categoryId);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
