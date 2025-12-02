using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Reviews;

/// <summary>
/// Page model for viewing detailed review moderation information and history.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class DetailsModel : PageModel
{
    private readonly IReviewModerationService _moderationService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IReviewModerationService moderationService,
        ILogger<DetailsModel> logger)
    {
        _moderationService = moderationService;
        _logger = logger;
    }

    public ProductReview Review { get; set; } = null!;
    public List<ReviewModerationLog> ModerationHistory { get; set; } = new();
    public List<ReviewFlag> Flags { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int reviewId)
    {
        try
        {
            // Get the review by ID
            Review = await _moderationService.GetReviewByIdAsync(reviewId);

            if (Review == null)
            {
                ErrorMessage = "Review not found.";
                return RedirectToPage("./Index");
            }

            // Get moderation history
            ModerationHistory = await _moderationService.GetReviewModerationHistoryAsync(reviewId);

            // Get all flags for this review
            Flags = await _moderationService.GetFlagsByReviewIdAsync(reviewId, includeResolved: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading review details for review {ReviewId}", reviewId);
            ErrorMessage = "An error occurred while loading review details.";
            return RedirectToPage("./Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(int reviewId)
    {
        try
        {
            var adminUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            await _moderationService.ApproveReviewAsync(reviewId, adminUserId);
            SuccessMessage = "Review approved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving review {ReviewId}", reviewId);
            ErrorMessage = "An error occurred while approving the review.";
        }

        return RedirectToPage(new { reviewId });
    }

    public async Task<IActionResult> OnPostRejectAsync(int reviewId, string reason)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                ErrorMessage = "Please provide a reason for rejecting the review.";
                return RedirectToPage(new { reviewId });
            }

            var adminUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            await _moderationService.RejectReviewAsync(reviewId, adminUserId, reason);
            SuccessMessage = "Review rejected successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting review {ReviewId}", reviewId);
            ErrorMessage = "An error occurred while rejecting the review.";
        }

        return RedirectToPage(new { reviewId });
    }
}
