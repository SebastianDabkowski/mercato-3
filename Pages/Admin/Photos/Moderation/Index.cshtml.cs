using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Photos.Moderation;

/// <summary>
/// Page model for admin photo moderation queue.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class IndexModel : PageModel
{
    private readonly IPhotoModerationService _moderationService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IPhotoModerationService moderationService,
        ILogger<IndexModel> logger)
    {
        _moderationService = moderationService;
        _logger = logger;
    }

    /// <summary>
    /// List of photos to display.
    /// </summary>
    public List<ProductImage> Photos { get; set; } = new();

    /// <summary>
    /// Statistics for photo moderation.
    /// </summary>
    public Dictionary<string, int> Stats { get; set; } = new();

    /// <summary>
    /// Current tab (pending, flagged, approved, rejected, all).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string CurrentTab { get; set; } = "pending";

    /// <summary>
    /// Optional product ID filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int? ProductId { get; set; }

    /// <summary>
    /// Optional store ID filter.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int? StoreId { get; set; }

    /// <summary>
    /// Success message to display.
    /// </summary>
    [TempData]
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Error message to display.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        // Get statistics
        Stats = await _moderationService.GetModerationStatsAsync();

        // Determine status and flagged filter based on current tab
        PhotoModerationStatus? status = null;
        bool flaggedOnly = false;

        switch (CurrentTab.ToLowerInvariant())
        {
            case "pending":
                status = PhotoModerationStatus.PendingReview;
                break;
            case "flagged":
                flaggedOnly = true;
                status = PhotoModerationStatus.Flagged;
                break;
            case "approved":
                status = PhotoModerationStatus.Approved;
                break;
            case "rejected":
                status = PhotoModerationStatus.Rejected;
                break;
            case "all":
                status = null;
                break;
        }

        // Get photos based on filters
        Photos = await _moderationService.GetPhotosByModerationStatusAsync(
            status: status,
            productId: ProductId,
            storeId: StoreId,
            flaggedOnly: flaggedOnly,
            page: 1,
            pageSize: 50);
    }

    public async Task<IActionResult> OnPostApproveAsync(int imageId)
    {
        try
        {
            var adminUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            await _moderationService.ApprovePhotoAsync(imageId, adminUserId);
            
            SuccessMessage = "Photo approved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving photo {ImageId}", imageId);
            ErrorMessage = "Failed to approve photo.";
        }

        return RedirectToPage(new { CurrentTab, ProductId, StoreId });
    }

    public async Task<IActionResult> OnPostRemoveAsync(int imageId, string reason)
    {
        try
        {
            var adminUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            await _moderationService.RemovePhotoAsync(imageId, adminUserId, reason);
            
            SuccessMessage = "Photo removed successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing photo {ImageId}", imageId);
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { CurrentTab, ProductId, StoreId });
    }

    public async Task<IActionResult> OnPostBulkApproveAsync(List<int> selectedPhotoIds)
    {
        try
        {
            var adminUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var count = await _moderationService.BulkApprovePhotosAsync(selectedPhotoIds, adminUserId);
            
            SuccessMessage = $"{count} photo(s) approved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk approve photos");
            ErrorMessage = "Failed to approve photos.";
        }

        return RedirectToPage(new { CurrentTab, ProductId, StoreId });
    }
}
