using System.ComponentModel.DataAnnotations;
using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MercatoApp.Pages.Admin.Categories;

[Authorize(Policy = PolicyNames.AdminOnly)]
public class EditModel : PageModel
{
    private readonly ICategoryService _categoryService;

    public EditModel(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public Category? Category { get; set; }

    public List<SelectListItem> ParentCategories { get; set; } = new();

    public int ProductCount { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    public class InputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [MaxLength(100, ErrorMessage = "Category name must be 100 characters or less.")]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Parent Category")]
        public int? ParentCategoryId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Display order must be a positive number.")]
        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Category = await _categoryService.GetCategoryByIdAsync(id);
        if (Category == null)
        {
            return NotFound();
        }

        Input = new InputModel
        {
            Id = Category.Id,
            Name = Category.Name,
            ParentCategoryId = Category.ParentCategoryId,
            DisplayOrder = Category.DisplayOrder,
            IsActive = Category.IsActive
        };

        ProductCount = await _categoryService.GetProductCountForCategoryAsync(id);

        await LoadParentCategoriesAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Category = await _categoryService.GetCategoryByIdAsync(Input.Id);
            ProductCount = await _categoryService.GetProductCountForCategoryAsync(Input.Id);
            await LoadParentCategoriesAsync(Input.Id);
            return Page();
        }

        var data = new UpdateCategoryData
        {
            Name = Input.Name,
            ParentCategoryId = Input.ParentCategoryId,
            DisplayOrder = Input.DisplayOrder,
            IsActive = Input.IsActive
        };

        var result = await _categoryService.UpdateCategoryAsync(Input.Id, data);

        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            Category = await _categoryService.GetCategoryByIdAsync(Input.Id);
            ProductCount = await _categoryService.GetProductCountForCategoryAsync(Input.Id);
            await LoadParentCategoriesAsync(Input.Id);
            return Page();
        }

        TempData["SuccessMessage"] = $"Category '{result.Category!.Name}' updated successfully.";
        return RedirectToPage("Index");
    }

    private async Task LoadParentCategoriesAsync(int excludeCategoryId)
    {
        var categories = await _categoryService.GetActiveCategoriesForSelectionAsync();
        
        // Exclude current category and its descendants from parent options
        var excludeIds = new HashSet<int> { excludeCategoryId };
        await CollectDescendantIds(excludeCategoryId, excludeIds);

        ParentCategories = categories
            .Where(c => !excludeIds.Contains(c.Id))
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

    private async Task CollectDescendantIds(int categoryId, HashSet<int> ids)
    {
        var allCategories = await _categoryService.GetAllCategoriesAsync();
        CollectDescendantsRecursive(categoryId, allCategories, ids);
    }

    private void CollectDescendantsRecursive(int categoryId, List<Category> allCategories, HashSet<int> ids)
    {
        var children = allCategories.Where(c => c.ParentCategoryId == categoryId);
        foreach (var child in children)
        {
            ids.Add(child.Id);
            CollectDescendantsRecursive(child.Id, allCategories, ids);
        }
    }
}
