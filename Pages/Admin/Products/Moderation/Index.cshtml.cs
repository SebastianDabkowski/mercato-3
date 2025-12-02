using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Products.Moderation;

/// <summary>
/// Page model for admin product moderation dashboard.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly IProductModerationService _moderationService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IProductModerationService moderationService,
        ICategoryService categoryService,
        ILogger<IndexModel> logger)
    {
        _moderationService = moderationService;
        _categoryService = categoryService;
        _logger = logger;
    }

    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public Dictionary<string, int> Stats { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string CurrentTab { get; set; } = "pending";
    public int? SelectedCategoryId { get; set; }

    [BindProperty]
    public List<int> SelectedProductIds { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? tab, int? categoryId, int page = 1)
    {
        CurrentTab = tab ?? "pending";
        SelectedCategoryId = categoryId;
        CurrentPage = page;

        try
        {
            // Get moderation statistics
            Stats = await _moderationService.GetModerationStatsAsync();

            // Get all categories for filter
            Categories = await _categoryService.GetAllCategoriesAsync();

            // Load data based on selected tab
            ProductModerationStatus? status = CurrentTab switch
            {
                "pending" => ProductModerationStatus.Pending,
                "approved" => ProductModerationStatus.Approved,
                "rejected" => ProductModerationStatus.Rejected,
                "all" => null,
                _ => ProductModerationStatus.Pending
            };

            Products = await _moderationService.GetProductsByModerationStatusAsync(
                status,
                categoryId,
                CurrentPage,
                PageSize
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product moderation dashboard");
            ErrorMessage = "An error occurred while loading the product moderation data.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(int productId, string returnTab = "pending", int? categoryId = null)
    {
        try
        {
            var adminUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            await _moderationService.ApproveProductAsync(productId, adminUserId);
            SuccessMessage = "Product approved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving product {ProductId}", productId);
            ErrorMessage = "An error occurred while approving the product.";
        }

        return RedirectToPage(new { tab = returnTab, categoryId });
    }

    public async Task<IActionResult> OnPostRejectAsync(int productId, string reason, string returnTab = "pending", int? categoryId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                ErrorMessage = "Please provide a reason for rejecting the product.";
                return RedirectToPage(new { tab = returnTab, categoryId });
            }

            var adminUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            await _moderationService.RejectProductAsync(productId, adminUserId, reason);
            SuccessMessage = "Product rejected successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting product {ProductId}", productId);
            ErrorMessage = "An error occurred while rejecting the product.";
        }

        return RedirectToPage(new { tab = returnTab, categoryId });
    }

    public async Task<IActionResult> OnPostBulkApproveAsync(string returnTab = "pending", int? categoryId = null)
    {
        try
        {
            if (SelectedProductIds == null || SelectedProductIds.Count == 0)
            {
                ErrorMessage = "Please select at least one product to approve.";
                return RedirectToPage(new { tab = returnTab, categoryId });
            }

            var adminUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var count = await _moderationService.BulkApproveProductsAsync(SelectedProductIds, adminUserId);
            SuccessMessage = $"Successfully approved {count} product(s).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk approving products");
            ErrorMessage = "An error occurred while bulk approving products.";
        }

        return RedirectToPage(new { tab = returnTab, categoryId });
    }

    public async Task<IActionResult> OnPostBulkRejectAsync(string reason, string returnTab = "pending", int? categoryId = null)
    {
        try
        {
            if (SelectedProductIds == null || SelectedProductIds.Count == 0)
            {
                ErrorMessage = "Please select at least one product to reject.";
                return RedirectToPage(new { tab = returnTab, categoryId });
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                ErrorMessage = "Please provide a reason for rejecting the products.";
                return RedirectToPage(new { tab = returnTab, categoryId });
            }

            var adminUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var count = await _moderationService.BulkRejectProductsAsync(SelectedProductIds, adminUserId, reason);
            SuccessMessage = $"Successfully rejected {count} product(s).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk rejecting products");
            ErrorMessage = "An error occurred while bulk rejecting products.";
        }

        return RedirectToPage(new { tab = returnTab, categoryId });
    }
}
