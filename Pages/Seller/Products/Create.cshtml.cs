using System.ComponentModel.DataAnnotations;
using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MercatoApp.Pages.Seller.Products;

[Authorize(Policy = PolicyNames.SellerOnly)]
public class CreateModel : PageModel
{
    private readonly IProductService _productService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ICategoryService _categoryService;

    public CreateModel(
        IProductService productService,
        IStoreProfileService storeProfileService,
        ICategoryService categoryService)
    {
        _productService = productService;
        _storeProfileService = storeProfileService;
        _categoryService = categoryService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public Store? Store { get; set; }

    public List<CategorySelectOption> Categories { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Represents a category option for selection in a dropdown.
    /// </summary>
    public class CategorySelectOption
    {
        public int Id { get; set; }
        public string FullPath { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Product title is required.")]
        [MaxLength(200, ErrorMessage = "Title must be 200 characters or less.")]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000, ErrorMessage = "Description must be 2000 characters or less.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be between 0.01 and 999,999.99.")]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
        [Display(Name = "Stock")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [MaxLength(100, ErrorMessage = "Category must be 100 characters or less.")]
        [Display(Name = "Category")]
        public string Category { get; set; } = string.Empty;

        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        [Range(0, 1000, ErrorMessage = "Weight must be between 0 and 1000 kg.")]
        [Display(Name = "Weight (kg)")]
        public decimal? Weight { get; set; }

        [Range(0, 500, ErrorMessage = "Length must be between 0 and 500 cm.")]
        [Display(Name = "Length (cm)")]
        public decimal? Length { get; set; }

        [Range(0, 500, ErrorMessage = "Width must be between 0 and 500 cm.")]
        [Display(Name = "Width (cm)")]
        public decimal? Width { get; set; }

        [Range(0, 500, ErrorMessage = "Height must be between 0 and 500 cm.")]
        [Display(Name = "Height (cm)")]
        public decimal? Height { get; set; }

        [MaxLength(500, ErrorMessage = "Shipping methods must be 500 characters or less.")]
        [Display(Name = "Shipping Methods")]
        public string? ShippingMethods { get; set; }

        [MaxLength(2000, ErrorMessage = "Image URLs must be 2000 characters or less.")]
        [Display(Name = "Image URLs")]
        public string? ImageUrls { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        Store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (Store == null)
        {
            return RedirectToPage("/Seller/OnboardingStep1");
        }

        await LoadCategoriesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToPage("/Account/Login");
        }

        Store = await _storeProfileService.GetStoreAsync(userId.Value);
        if (Store == null)
        {
            return RedirectToPage("/Seller/OnboardingStep1");
        }

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return Page();
        }

        var data = new CreateProductData
        {
            Title = Input.Title,
            Description = Input.Description,
            Price = Input.Price,
            Stock = Input.Stock,
            Category = Input.Category,
            CategoryId = Input.CategoryId,
            Weight = Input.Weight,
            Length = Input.Length,
            Width = Input.Width,
            Height = Input.Height,
            ShippingMethods = Input.ShippingMethods,
            ImageUrls = Input.ImageUrls
        };

        var result = await _productService.CreateProductAsync(Store.Id, data);

        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            await LoadCategoriesAsync();
            return Page();
        }

        TempData["SuccessMessage"] = "Product created successfully.";
        return RedirectToPage("Index");
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _categoryService.GetActiveCategoriesForSelectionAsync();
        Categories = categories
            .Select(c => new CategorySelectOption
            {
                Id = c.Id,
                FullPath = c.FullPath,
                DisplayName = c.DisplayName
            })
            .ToList();
    }
}
