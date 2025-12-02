using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Products.Moderation;

/// <summary>
/// Page model for product moderation details.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class DetailsModel : PageModel
{
    private readonly IProductModerationService _moderationService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IProductModerationService moderationService,
        ILogger<DetailsModel> logger)
    {
        _moderationService = moderationService;
        _logger = logger;
    }

    public Product? Product { get; set; }
    public List<ProductModerationLog> ModerationHistory { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int productId)
    {
        try
        {
            Product = await _moderationService.GetProductByIdAsync(productId);

            if (Product == null)
            {
                ErrorMessage = "Product not found.";
                return RedirectToPage("./Index");
            }

            ModerationHistory = await _moderationService.GetProductModerationHistoryAsync(productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product details for product {ProductId}", productId);
            ErrorMessage = "An error occurred while loading product details.";
            return RedirectToPage("./Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(int productId, string? reason)
    {
        try
        {
            var adminUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            await _moderationService.ApproveProductAsync(productId, adminUserId, reason);
            SuccessMessage = "Product approved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving product {ProductId}", productId);
            ErrorMessage = "An error occurred while approving the product.";
        }

        return RedirectToPage(new { productId });
    }

    public async Task<IActionResult> OnPostRejectAsync(int productId, string reason)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                ErrorMessage = "Please provide a reason for rejecting the product.";
                return RedirectToPage(new { productId });
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

        return RedirectToPage(new { productId });
    }
}
