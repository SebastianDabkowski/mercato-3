using MercatoApp.Authorization;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Photos.Moderation;

/// <summary>
/// Page model for viewing photo moderation details.
/// </summary>
[Authorize(Policy = PolicyNames.AdminOnly)]
public class DetailsModel : PageModel
{
    private readonly IPhotoModerationService _moderationService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IPhotoModerationService moderationService,
        ILogger<DetailsModel> logger)
    {
        _moderationService = moderationService;
        _logger = logger;
    }

    /// <summary>
    /// Product image to display.
    /// </summary>
    public ProductImage Photo { get; set; } = null!;

    /// <summary>
    /// Flags on this photo.
    /// </summary>
    public List<PhotoFlag> Flags { get; set; } = new();

    /// <summary>
    /// Moderation history for this photo.
    /// </summary>
    public List<PhotoModerationLog> ModerationHistory { get; set; } = new();

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

    public async Task<IActionResult> OnGetAsync(int imageId)
    {
        var photo = await _moderationService.GetPhotoByIdAsync(imageId);
        
        if (photo == null)
        {
            return NotFound();
        }

        Photo = photo;
        Flags = await _moderationService.GetPhotoFlagsAsync(imageId);
        ModerationHistory = await _moderationService.GetPhotoModerationHistoryAsync(imageId);

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(int imageId, string? reason)
    {
        try
        {
            var adminUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            await _moderationService.ApprovePhotoAsync(imageId, adminUserId, reason);
            
            SuccessMessage = "Photo approved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving photo {ImageId}", imageId);
            ErrorMessage = "Failed to approve photo.";
        }

        return RedirectToPage(new { imageId });
    }

    public async Task<IActionResult> OnPostRemoveAsync(int imageId, string reason)
    {
        try
        {
            var adminUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            await _moderationService.RemovePhotoAsync(imageId, adminUserId, reason);
            
            SuccessMessage = "Photo removed successfully. The seller has been notified.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing photo {ImageId}", imageId);
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { imageId });
    }
}
