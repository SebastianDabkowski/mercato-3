using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages;

public class IndexModel : PageModel
{
    private readonly ICategoryService _categoryService;

    public IndexModel(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public List<CategoryTreeItem> RootCategories { get; set; } = new();

    public async Task OnGetAsync()
    {
        var allCategories = await _categoryService.GetCategoryTreeAsync();
        RootCategories = allCategories.Where(c => c.IsActive).ToList();
    }
}
