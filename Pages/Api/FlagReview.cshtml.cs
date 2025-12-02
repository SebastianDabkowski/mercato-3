using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MercatoApp.Pages.Api;

/// <summary>
/// API endpoint for flagging product reviews.
/// </summary>
[Authorize]
public class FlagReviewModel : PageModel
{
    private readonly IReviewModerationService _moderationService;
    private readonly ILogger<FlagReviewModel> _logger;

    public FlagReviewModel(
        IReviewModerationService moderationService,
        ILogger<FlagReviewModel> logger)
    {
        _moderationService = moderationService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(int reviewId, string reason, string? details)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return new JsonResult(new { success = false, message = "User not authenticated" })
            {
                StatusCode = 401
            };
        }

        // Parse the reason string to enum
        if (!Enum.TryParse<ReviewFlagReason>(reason, out var flagReason))
        {
            return new JsonResult(new { success = false, message = "Invalid flag reason" })
            {
                StatusCode = 400
            };
        }

        try
        {
            var flag = await _moderationService.FlagReviewAsync(
                reviewId,
                flagReason,
                details,
                userId,
                isAutomated: false
            );

            _logger.LogInformation("User {UserId} flagged review {ReviewId} for {Reason}", 
                userId, reviewId, flagReason);

            return new JsonResult(new { success = true, message = "Review flagged successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flagging review {ReviewId}", reviewId);
            return new JsonResult(new { success = false, message = "An error occurred while flagging the review" })
            {
                StatusCode = 500
            };
        }
    }
}
