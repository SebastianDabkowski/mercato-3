using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Categories.Attributes;

[Authorize(Policy = PolicyNames.AdminOnly)]
public class EditModel : PageModel
{
    private readonly ICategoryAttributeService _attributeService;

    public EditModel(ICategoryAttributeService attributeService)
    {
        _attributeService = attributeService;
    }

    public CategoryAttribute? Attribute { get; set; }
    public int ProductCount { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<string> Errors { get; set; } = new();

    public class InputModel
    {
        public string Name { get; set; } = string.Empty;
        public string? DisplayLabel { get; set; }
        public string? Description { get; set; }
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

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Attribute = await _attributeService.GetAttributeByIdAsync(id);

        if (Attribute == null)
        {
            return NotFound();
        }

        ProductCount = await _attributeService.GetProductCountForAttributeAsync(id);

        // Populate input model
        Input.Name = Attribute.Name;
        Input.DisplayLabel = Attribute.DisplayLabel;
        Input.Description = Attribute.Description;
        Input.IsRequired = Attribute.IsRequired;
        Input.IsFilterable = Attribute.IsFilterable;
        Input.IsSearchable = Attribute.IsSearchable;
        Input.DisplayOrder = Attribute.DisplayOrder;
        Input.ValidationPattern = Attribute.ValidationPattern;
        Input.ValidationMessage = Attribute.ValidationMessage;
        Input.MinValue = Attribute.MinValue;
        Input.MaxValue = Attribute.MaxValue;
        Input.Unit = Attribute.Unit;

        // Populate options text
        if (Attribute.Options.Any())
        {
            Input.OptionsText = string.Join("\n", Attribute.Options.Select(o => o.Value));
        }

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

        var data = new UpdateCategoryAttributeData
        {
            Name = Input.Name,
            DisplayLabel = Input.DisplayLabel,
            Description = Input.Description,
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

        var result = await _attributeService.UpdateAttributeAsync(id, data);

        if (result.Success)
        {
            TempData["SuccessMessage"] = $"Attribute '{result.CategoryAttribute?.Name}' updated successfully.";
            return RedirectToPage("Index", new { categoryId = Attribute.CategoryId });
        }

        Errors = result.Errors;
        return Page();
    }
}
