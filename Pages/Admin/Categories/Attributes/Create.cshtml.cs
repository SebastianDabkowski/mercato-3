using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Categories.Attributes;

[Authorize(Policy = PolicyNames.AdminOnly)]
public class CreateModel : PageModel
{
    private readonly ICategoryService _categoryService;
    private readonly ICategoryAttributeService _attributeService;

    public CreateModel(
        ICategoryService categoryService,
        ICategoryAttributeService attributeService)
    {
        _categoryService = categoryService;
        _attributeService = attributeService;
    }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<string> Errors { get; set; } = new();

    public class InputModel
    {
        public string Name { get; set; } = string.Empty;
        public string? DisplayLabel { get; set; }
        public string? Description { get; set; }
        public AttributeType AttributeType { get; set; }
        public bool IsRequired { get; set; }
        public bool IsFilterable { get; set; }
        public bool IsSearchable { get; set; }
        public int DisplayOrder { get; set; }
        public string? ValidationPattern { get; set; }
        public string? ValidationMessage { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public string? Unit { get; set; }
        public string? OptionsText { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int categoryId)
    {
        CategoryId = categoryId;
        Category = await _categoryService.GetCategoryByIdAsync(categoryId);

        if (Category == null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int categoryId)
    {
        CategoryId = categoryId;
        Category = await _categoryService.GetCategoryByIdAsync(categoryId);

        if (Category == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Parse options from text area
        List<string>? options = null;
        if (!string.IsNullOrWhiteSpace(Input.OptionsText))
        {
            options = Input.OptionsText
                .Split('\n')
                .Select(o => o.Trim())
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .ToList();
        }

        var data = new CreateCategoryAttributeData
        {
            CategoryId = categoryId,
            Name = Input.Name,
            DisplayLabel = Input.DisplayLabel,
            Description = Input.Description,
            AttributeType = Input.AttributeType,
            IsRequired = Input.IsRequired,
            IsFilterable = Input.IsFilterable,
            IsSearchable = Input.IsSearchable,
            DisplayOrder = Input.DisplayOrder,
            ValidationPattern = Input.ValidationPattern,
            ValidationMessage = Input.ValidationMessage,
            MinValue = Input.MinValue,
            MaxValue = Input.MaxValue,
            Unit = Input.Unit,
            Options = options
        };

        var result = await _attributeService.CreateAttributeAsync(data);

        if (result.Success)
        {
            TempData["SuccessMessage"] = $"Attribute '{result.CategoryAttribute?.Name}' created successfully.";
            return RedirectToPage("Index", new { categoryId });
        }

        Errors = result.Errors;
        return Page();
    }
}
