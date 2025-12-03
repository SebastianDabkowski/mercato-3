using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Categories.Attributes;

[Authorize(Policy = PolicyNames.AdminOnly)]
public class DeleteModel : PageModel
{
    private readonly ICategoryAttributeService _attributeService;

    public DeleteModel(ICategoryAttributeService attributeService)
    {
        _attributeService = attributeService;
    }

    public CategoryAttribute? Attribute { get; set; }
    public int ProductCount { get; set; }
    public List<string> Errors { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Attribute = await _attributeService.GetAttributeByIdAsync(id);

        if (Attribute == null)
        {
            return NotFound();
        }

        ProductCount = await _attributeService.GetProductCountForAttributeAsync(id);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        Attribute = await _attributeService.GetAttributeByIdAsync(id);

        if (Attribute == null)
        {
            return NotFound();
        }

        ProductCount = await _attributeService.GetProductCountForAttributeAsync(id);

        var result = await _attributeService.DeleteAttributeAsync(id);

        if (result.Success)
        {
            TempData["SuccessMessage"] = $"Attribute '{result.CategoryAttribute?.Name}' has been deleted.";
            return RedirectToPage("Index", new { categoryId = Attribute.CategoryId });
        }

        Errors = result.Errors;
        return Page();
    }
}
