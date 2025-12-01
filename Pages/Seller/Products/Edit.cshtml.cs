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
public class EditModel : PageModel
{
    private readonly IProductService _productService;
    private readonly IStoreProfileService _storeProfileService;
    private readonly ICategoryService _categoryService;
    private readonly IProductImageService _productImageService;

    public EditModel(
        IProductService productService,
        IStoreProfileService storeProfileService,
        ICategoryService categoryService,
        IProductImageService productImageService)
    {
        _productService = productService;
        _storeProfileService = storeProfileService;
        _categoryService = categoryService;
        _productImageService = productImageService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public List<IFormFile>? UploadedImages { get; set; }

    public Store? Store { get; set; }
    public Product? Product { get; set; }
    public List<ProductImage> ProductImages { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    public List<SelectListItem> StatusOptions { get; set; } = new();

    public List<CategorySelectOption> Categories { get; set; } = new();

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

        [Required(ErrorMessage = "Status is required.")]
        [Display(Name = "Status")]
        public ProductStatus Status { get; set; }

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

    public async Task<IActionResult> OnGetAsync(int id)
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

        // Get the product, ensuring it belongs to the seller's store
        Product = await _productService.GetProductByIdAsync(id, Store.Id);
        if (Product == null)
        {
            TempData["ErrorMessage"] = "Product not found or you do not have permission to edit it.";
            return RedirectToPage("Index");
        }

        // Cannot edit archived products
        if (Product.Status == ProductStatus.Archived)
        {
            TempData["ErrorMessage"] = "Cannot edit an archived product.";
            return RedirectToPage("Index");
        }

        // Load product images
        ProductImages = await _productImageService.GetProductImagesAsync(id, Store.Id);

        // Populate the form
        Input = new InputModel
        {
            Title = Product.Title,
            Description = Product.Description,
            Price = Product.Price,
            Stock = Product.Stock,
            Category = Product.Category,
            CategoryId = Product.CategoryId,
            Status = Product.Status,
            Weight = Product.Weight,
            Length = Product.Length,
            Width = Product.Width,
            Height = Product.Height,
            ShippingMethods = Product.ShippingMethods,
            ImageUrls = Product.ImageUrls
        };

        PopulateStatusOptions();
        await LoadCategoriesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
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

        // Verify the product still exists and belongs to this store
        Product = await _productService.GetProductByIdAsync(id, Store.Id);
        if (Product == null)
        {
            TempData["ErrorMessage"] = "Product not found or you do not have permission to edit it.";
            return RedirectToPage("Index");
        }

        // Load product images
        ProductImages = await _productImageService.GetProductImagesAsync(id, Store.Id);

        PopulateStatusOptions();
        await LoadCategoriesAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var data = new UpdateProductData
        {
            Title = Input.Title,
            Description = Input.Description,
            Price = Input.Price,
            Stock = Input.Stock,
            Category = Input.Category,
            CategoryId = Input.CategoryId,
            Status = Input.Status,
            Weight = Input.Weight,
            Length = Input.Length,
            Width = Input.Width,
            Height = Input.Height,
            ShippingMethods = Input.ShippingMethods,
            ImageUrls = Input.ImageUrls
        };

        var result = await _productService.UpdateProductAsync(id, Store.Id, data, userId.Value);

        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            return Page();
        }

        TempData["SuccessMessage"] = "Product updated successfully.";
        return RedirectToPage("Index");
    }

    public async Task<IActionResult> OnPostUploadImagesAsync(int id)
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

        Product = await _productService.GetProductByIdAsync(id, Store.Id);
        if (Product == null)
        {
            TempData["ErrorMessage"] = "Product not found.";
            return RedirectToPage("Index");
        }

        if (UploadedImages == null || UploadedImages.Count == 0)
        {
            TempData["ErrorMessage"] = "Please select at least one image to upload.";
            return RedirectToPage("Edit", new { id });
        }

        var uploadErrors = new List<string>();
        var uploadedCount = 0;

        foreach (var file in UploadedImages)
        {
            using var stream = file.OpenReadStream();
            var result = await _productImageService.UploadImageAsync(
                id,
                Store.Id,
                stream,
                file.FileName,
                file.ContentType,
                file.Length);

            if (result.Success)
            {
                uploadedCount++;
            }
            else
            {
                uploadErrors.AddRange(result.Errors.Select(e => $"{file.FileName}: {e}"));
            }
        }

        if (uploadedCount > 0)
        {
            TempData["SuccessMessage"] = $"Successfully uploaded {uploadedCount} image(s).";
        }

        if (uploadErrors.Count > 0)
        {
            TempData["ErrorMessage"] = string.Join(" ", uploadErrors);
        }

        return RedirectToPage("Edit", new { id });
    }

    public async Task<IActionResult> OnPostDeleteImageAsync(int id, int imageId)
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

        var result = await _productImageService.DeleteImageAsync(imageId, Store.Id);
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Image deleted successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
        }

        return RedirectToPage("Edit", new { id });
    }

    public async Task<IActionResult> OnPostSetMainImageAsync(int id, int imageId)
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

        var result = await _productImageService.SetMainImageAsync(imageId, Store.Id);
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Main image updated successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors);
        }

        return RedirectToPage("Edit", new { id });
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

    private void PopulateStatusOptions()
    {
        // Get allowed transitions from the current product status
        var currentStatus = Product?.Status ?? ProductStatus.Draft;
        var allowedTransitions = ProductWorkflowService.GetAllowedTransitionsStatic(currentStatus, isAdmin: false);

        StatusOptions = new List<SelectListItem>
        {
            // Always include current status as an option
            new SelectListItem 
            { 
                Value = currentStatus.ToString(), 
                Text = GetStatusDisplayText(currentStatus),
                Selected = true
            }
        };

        // Add allowed transitions
        foreach (var status in allowedTransitions)
        {
            if (status != ProductStatus.Archived) // Archived handled separately via Delete page
            {
                StatusOptions.Add(new SelectListItem
                {
                    Value = status.ToString(),
                    Text = GetStatusDisplayText(status)
                });
            }
        }
    }

    private static string GetStatusDisplayText(ProductStatus status)
    {
        return status switch
        {
            ProductStatus.Draft => "Draft",
            ProductStatus.Active => "Active",
            ProductStatus.Suspended => "Suspended",
            ProductStatus.Archived => "Archived",
            _ => status.ToString()
        };
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
