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
    private readonly ISellerRatingModerationService _sellerRatingModerationService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IReviewModerationService moderationService,
        ISellerRatingModerationService sellerRatingModerationService,
        ILogger<IndexModel> logger)
    {
        _moderationService = moderationService;
        _sellerRatingModerationService = sellerRatingModerationService;
        _logger = logger;
    }

    public List<ReviewFlag> FlaggedProductReviews { get; set; } = new();
    public List<SellerRatingFlag> FlaggedSellerRatings { get; set; } = new();
    public List<ProductReview> ProductReviews { get; set; } = new();
    public List<SellerRating> SellerRatings { get; set; } = new();
    public Dictionary<string, int> ProductStats { get; set; } = new();
    public Dictionary<string, int> SellerStats { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string CurrentTab { get; set; } = "flagged";
    public string ReviewType { get; set; } = "all"; // all, product, seller

    public async Task<IActionResult> OnGetAsync(string? tab, string? type, int page = 1)
    {
        CurrentTab = tab ?? "flagged";
        ReviewType = type ?? "all";
        CurrentPage = page;

        try
        {
            // Get moderation statistics for both types
            ProductStats = await _moderationService.GetModerationStatsAsync();
            SellerStats = await _sellerRatingModerationService.GetModerationStatsAsync();

            // Load data based on selected tab and type
            if (CurrentTab == "flagged")
            {
                if (ReviewType == "all" || ReviewType == "product")
                {
                    FlaggedProductReviews = await _moderationService.GetFlaggedReviewsAsync(includeResolved: false);
                }
                if (ReviewType == "all" || ReviewType == "seller")
                {
                    FlaggedSellerRatings = await _sellerRatingModerationService.GetFlaggedRatingsAsync(includeResolved: false);
                }
            }
            else
            {
                var status = CurrentTab switch
                {
                    "pending" => ReviewModerationStatus.PendingReview,
                    "approved" => ReviewModerationStatus.Approved,
                    "rejected" => ReviewModerationStatus.Rejected,
                    _ => ReviewModerationStatus.PendingReview
                };

                if (ReviewType == "all" || ReviewType == "product")
                {
                    ProductReviews = await _moderationService.GetReviewsByStatusAsync(status, CurrentPage, PageSize);
                }
                if (ReviewType == "all" || ReviewType == "seller")
                {
                    SellerRatings = await _sellerRatingModerationService.GetRatingsByStatusAsync(status, CurrentPage, PageSize);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading review moderation dashboard");
            ErrorMessage = "An error occurred while loading the review moderation data.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostApproveProductReviewAsync(int reviewId, string returnTab = "flagged")
    {
        try
        {
            var adminUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            await _moderationService.ApproveReviewAsync(reviewId, adminUserId);
            SuccessMessage = "Product review approved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving product review {ReviewId}", reviewId);
            ErrorMessage = "An error occurred while approving the product review.";
        }

        return RedirectToPage(new { tab = returnTab });
    }

    public async Task<IActionResult> OnPostRejectProductReviewAsync(int reviewId, string reason, string returnTab = "flagged")
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
            SuccessMessage = "Product review rejected successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting product review {ReviewId}", reviewId);
            ErrorMessage = "An error occurred while rejecting the product review.";
        }

        return RedirectToPage(new { tab = returnTab });
    }

    public async Task<IActionResult> OnPostApproveSellerRatingAsync(int ratingId, string returnTab = "flagged")
    {
        try
        {
            var adminUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            await _sellerRatingModerationService.ApproveRatingAsync(ratingId, adminUserId);
            SuccessMessage = "Seller rating approved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving seller rating {RatingId}", ratingId);
            ErrorMessage = "An error occurred while approving the seller rating.";
        }

        return RedirectToPage(new { tab = returnTab });
    }

    public async Task<IActionResult> OnPostRejectSellerRatingAsync(int ratingId, string reason, string returnTab = "flagged")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                ErrorMessage = "Please provide a reason for rejecting the rating.";
                return RedirectToPage(new { tab = returnTab });
            }

            var adminUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            await _sellerRatingModerationService.RejectRatingAsync(ratingId, adminUserId, reason);
            SuccessMessage = "Seller rating rejected successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting seller rating {RatingId}", ratingId);
            ErrorMessage = "An error occurred while rejecting the seller rating.";
        }

        return RedirectToPage(new { tab = returnTab });
    }

    public async Task<IActionResult> OnPostResolveProductFlagAsync(int flagId, string returnTab = "flagged")
    {
        try
        {
            var adminUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            await _moderationService.ResolveFlagAsync(flagId, adminUserId);
            SuccessMessage = "Product review flag resolved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving product flag {FlagId}", flagId);
            ErrorMessage = "An error occurred while resolving the product flag.";
        }

        return RedirectToPage(new { tab = returnTab });
    }

    public async Task<IActionResult> OnPostResolveSellerFlagAsync(int flagId, string returnTab = "flagged")
    {
        try
        {
            var adminUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            await _sellerRatingModerationService.ResolveFlagAsync(flagId, adminUserId);
            SuccessMessage = "Seller rating flag resolved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving seller flag {FlagId}", flagId);
            ErrorMessage = "An error occurred while resolving the seller flag.";
        }

        return RedirectToPage(new { tab = returnTab });
    }
}
