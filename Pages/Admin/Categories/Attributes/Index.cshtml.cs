using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Categories.Attributes;

[Authorize(Policy = PolicyNames.AdminOnly)]
public class IndexModel : PageModel
{
    private readonly ICategoryService _categoryService;
    private readonly ICategoryAttributeService _attributeService;

    public IndexModel(
        ICategoryService categoryService,
        ICategoryAttributeService attributeService)
    {
        _categoryService = categoryService;
        _attributeService = attributeService;
    }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public List<CategoryAttribute> Attributes { get; set; } = new();
    public Dictionary<int, int> ProductCounts { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int categoryId)
    {
        CategoryId = categoryId;
        Category = await _categoryService.GetCategoryByIdAsync(categoryId);

        if (Category == null)
        {
            return NotFound();
        }

        Attributes = await _attributeService.GetAttributesForCategoryAsync(categoryId);

        // Get product counts for all attributes in a single query
        if (Attributes.Count > 0)
        {
            var attributeIds = Attributes.Select(a => a.Id);
            ProductCounts = await _attributeService.GetProductCountsForAttributesAsync(attributeIds);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDeprecateAsync(int id)
    {
        var result = await _attributeService.DeprecateAttributeAsync(id);

        if (result.Success && result.CategoryAttribute != null)
        {
            SuccessMessage = $"Attribute '{result.CategoryAttribute.Name}' has been deprecated.";
            return RedirectToPage(new { categoryId = result.CategoryAttribute.CategoryId });
        }

        ErrorMessage = string.Join(", ", result.Errors);
        return RedirectToPage(new { categoryId = CategoryId });
    }

    public async Task<IActionResult> OnPostRestoreAsync(int id)
    {
        var result = await _attributeService.RestoreAttributeAsync(id);

        if (result.Success && result.CategoryAttribute != null)
        {
            SuccessMessage = $"Attribute '{result.CategoryAttribute.Name}' has been restored.";
            return RedirectToPage(new { categoryId = result.CategoryAttribute.CategoryId });
        }

        ErrorMessage = string.Join(", ", result.Errors);
        return RedirectToPage(new { categoryId = CategoryId });
    }
}
