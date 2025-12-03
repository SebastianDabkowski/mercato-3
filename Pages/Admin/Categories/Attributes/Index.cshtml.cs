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

        // Get product counts for each attribute
        foreach (var attribute in Attributes)
        {
            var count = await _attributeService.GetProductCountForAttributeAsync(attribute.Id);
            ProductCounts[attribute.Id] = count;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDeprecateAsync(int id)
    {
        var result = await _attributeService.DeprecateAttributeAsync(id);

        if (result.Success)
        {
            SuccessMessage = $"Attribute '{result.CategoryAttribute?.Name}' has been deprecated.";
        }
        else
        {
            ErrorMessage = string.Join(", ", result.Errors);
        }

        // Get the category ID from the attribute
        var attribute = await _attributeService.GetAttributeByIdAsync(id);
        if (attribute == null)
        {
            return NotFound();
        }

        return RedirectToPage(new { categoryId = attribute.CategoryId });
    }

    public async Task<IActionResult> OnPostRestoreAsync(int id)
    {
        var result = await _attributeService.RestoreAttributeAsync(id);

        if (result.Success)
        {
            SuccessMessage = $"Attribute '{result.CategoryAttribute?.Name}' has been restored.";
        }
        else
        {
            ErrorMessage = string.Join(", ", result.Errors);
        }

        // Get the category ID from the attribute
        var attribute = await _attributeService.GetAttributeByIdAsync(id);
        if (attribute == null)
        {
            return NotFound();
        }

        return RedirectToPage(new { categoryId = attribute.CategoryId });
    }
}
