using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Categories;

[Authorize(Policy = PolicyNames.AdminOnly)]
public class DeleteModel : PageModel
{
    private readonly ICategoryService _categoryService;

    public DeleteModel(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public Category? Category { get; set; }

    public int ProductCount { get; set; }

    public bool HasChildCategories { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Category = await _categoryService.GetCategoryByIdAsync(id);
        if (Category == null)
        {
            return NotFound();
        }

        ProductCount = await _categoryService.GetProductCountForCategoryAsync(id);
        HasChildCategories = await _categoryService.HasChildCategoriesAsync(id);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        Category = await _categoryService.GetCategoryByIdAsync(id);
        if (Category == null)
        {
            return NotFound();
        }

        var result = await _categoryService.DeleteCategoryAsync(id);

        if (!result.Success)
        {
            ProductCount = await _categoryService.GetProductCountForCategoryAsync(id);
            HasChildCategories = await _categoryService.HasChildCategoriesAsync(id);
            
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
            return Page();
        }

        TempData["SuccessMessage"] = $"Category '{Category.Name}' deleted successfully.";
        return RedirectToPage("Index");
    }
}
