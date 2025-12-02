using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MercatoApp.Pages.Admin.Reviews;

/// <summary>
/// Page model for admin review moderation dashboard.
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly IReviewModerationService _moderationService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IReviewModerationService moderationService,
        ILogger<IndexModel> logger)
    {
        _moderationService = moderationService;
        _logger = logger;
    }

    public List<ReviewFlag> FlaggedReviews { get; set; } = new();
    public List<ProductReview> PendingReviews { get; set; } = new();
    public Dictionary<string, int> Stats { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string CurrentTab { get; set; } = "flagged";

    public async Task<IActionResult> OnGetAsync(string? tab, int page = 1)
    {
        CurrentTab = tab ?? "flagged";
        CurrentPage = page;

        try
        {
            // Get moderation statistics
            Stats = await _moderationService.GetModerationStatsAsync();

            // Load data based on selected tab
            if (CurrentTab == "flagged")
            {
                FlaggedReviews = await _moderationService.GetFlaggedReviewsAsync(includeResolved: false);
            }
            else if (CurrentTab == "pending")
            {
                PendingReviews = await _moderationService.GetReviewsByStatusAsync(
                    ReviewModerationStatus.PendingReview,
                    CurrentPage,
                    PageSize
                );
            }
            else if (CurrentTab == "approved")
            {
                PendingReviews = await _moderationService.GetReviewsByStatusAsync(
                    ReviewModerationStatus.Approved,
                    CurrentPage,
                    PageSize
                );
            }
            else if (CurrentTab == "rejected")
            {
                PendingReviews = await _moderationService.GetReviewsByStatusAsync(
                    ReviewModerationStatus.Rejected,
                    CurrentPage,
                    PageSize
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading review moderation dashboard");
            ErrorMessage = "An error occurred while loading the review moderation data.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(int reviewId, string returnTab = "flagged")
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

        return RedirectToPage(new { tab = returnTab });
    }

    public async Task<IActionResult> OnPostRejectAsync(int reviewId, string reason, string returnTab = "flagged")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                ErrorMessage = "Please provide a reason for rejecting the review.";
                return RedirectToPage(new { tab = returnTab });
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

        return RedirectToPage(new { tab = returnTab });
    }

    public async Task<IActionResult> OnPostToggleVisibilityAsync(int reviewId, bool isVisible, string returnTab = "flagged")
    {
        try
        {
            var adminUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            await _moderationService.ToggleReviewVisibilityAsync(reviewId, adminUserId, isVisible);
            SuccessMessage = $"Review visibility updated to {(isVisible ? "visible" : "hidden")}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling visibility for review {ReviewId}", reviewId);
            ErrorMessage = "An error occurred while updating review visibility.";
        }

        return RedirectToPage(new { tab = returnTab });
    }

    public async Task<IActionResult> OnPostResolveFlagAsync(int flagId, string returnTab = "flagged")
    {
        try
        {
            var adminUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            await _moderationService.ResolveFlagAsync(flagId, adminUserId);
            SuccessMessage = "Flag resolved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving flag {FlagId}", flagId);
            ErrorMessage = "An error occurred while resolving the flag.";
        }

        return RedirectToPage(new { tab = returnTab });
    }
}
