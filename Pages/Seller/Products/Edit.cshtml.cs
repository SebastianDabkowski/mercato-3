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

    public EditModel(
        IProductService productService,
        IStoreProfileService storeProfileService)
    {
        _productService = productService;
        _storeProfileService = storeProfileService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public Store? Store { get; set; }
    public Product? Product { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    public List<SelectListItem> StatusOptions { get; set; } = new();

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

        [Required(ErrorMessage = "Status is required.")]
        [Display(Name = "Status")]
        public ProductStatus Status { get; set; }
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

        // Populate the form
        Input = new InputModel
        {
            Title = Product.Title,
            Description = Product.Description,
            Price = Product.Price,
            Stock = Product.Stock,
            Category = Product.Category,
            Status = Product.Status
        };

        PopulateStatusOptions();
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

        PopulateStatusOptions();

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
            Status = Input.Status
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
        StatusOptions = new List<SelectListItem>
        {
            new SelectListItem { Value = ProductStatus.Draft.ToString(), Text = "Draft" },
            new SelectListItem { Value = ProductStatus.Active.ToString(), Text = "Active" },
            new SelectListItem { Value = ProductStatus.Inactive.ToString(), Text = "Inactive" }
        };
    }
}
