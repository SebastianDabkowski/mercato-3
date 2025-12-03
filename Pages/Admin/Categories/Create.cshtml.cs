using System.ComponentModel.DataAnnotations;
using MercatoApp.Authorization;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MercatoApp.Pages.Admin.Categories;

[Authorize(Policy = PolicyNames.AdminOnly)]
public class CreateModel : PageModel
{
    private readonly ICategoryService _categoryService;

    public CreateModel(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> ParentCategories { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Category name is required.")]
        [MaxLength(CategoryService.MaxNameLength, ErrorMessage = "Category name must be 100 characters or less.")]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(CategoryService.MaxDescriptionLength, ErrorMessage = "Description must be 500 characters or less.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Parent Category")]
        public int? ParentCategoryId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Display order must be a positive number.")]
        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadParentCategoriesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadParentCategoriesAsync();
            return Page();
        }

        var data = new CreateCategoryData
        {
            Name = Input.Name,
            Description = Input.Description,
            ParentCategoryId = Input.ParentCategoryId,
            DisplayOrder = Input.DisplayOrder
        };

        var result = await _categoryService.CreateCategoryAsync(data);

        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            await LoadParentCategoriesAsync();
            return Page();
        }

        TempData["SuccessMessage"] = $"Category '{result.Category!.Name}' created successfully.";
        return RedirectToPage("Index");
    }

    private async Task LoadParentCategoriesAsync()
    {
        var categories = await _categoryService.GetActiveCategoriesForSelectionAsync();
        ParentCategories = categories
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.FullPath
            })
            .ToList();
        
        ParentCategories.Insert(0, new SelectListItem
        {
            Value = "",
            Text = "-- No Parent (Root Category) --"
        });
    }
}
