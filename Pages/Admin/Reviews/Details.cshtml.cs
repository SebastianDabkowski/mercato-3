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
    private readonly IProductReviewService _reviewService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        IReviewModerationService moderationService,
        IProductReviewService reviewService,
        ILogger<DetailsModel> logger)
    {
        _moderationService = moderationService;
        _reviewService = reviewService;
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
            // Get all reviews to find the one we need (since we don't have a direct GetById method)
            var allReviews = await _reviewService.GetApprovedReviewsForProductAsync(0);
            Review = allReviews.FirstOrDefault(r => r.Id == reviewId);

            if (Review == null)
            {
                // Try getting from other statuses
                var pendingReviews = await _moderationService.GetReviewsByStatusAsync(ReviewModerationStatus.PendingReview, 1, 1000);
                Review = pendingReviews.FirstOrDefault(r => r.Id == reviewId);
            }

            if (Review == null)
            {
                var rejectedReviews = await _moderationService.GetReviewsByStatusAsync(ReviewModerationStatus.Rejected, 1, 1000);
                Review = rejectedReviews.FirstOrDefault(r => r.Id == reviewId);
            }

            if (Review == null)
            {
                ErrorMessage = "Review not found.";
                return RedirectToPage("./Index");
            }

            // Get moderation history
            ModerationHistory = await _moderationService.GetReviewModerationHistoryAsync(reviewId);

            // Get all flags for this review
            var allFlags = await _moderationService.GetFlaggedReviewsAsync(includeResolved: true);
            Flags = allFlags.Where(f => f.ProductReviewId == reviewId).ToList();
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
