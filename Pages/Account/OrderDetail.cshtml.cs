using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MercatoApp.Pages.Account;

/// <summary>
/// Page model for displaying detailed order information to a buyer.
/// </summary>
[Authorize]
public class OrderDetailModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IReturnRequestService _returnRequestService;
    private readonly IProductReviewService _reviewService;
    private readonly IReviewModerationService _moderationService;
    private readonly ISellerRatingService _sellerRatingService;
    private readonly IOrderMessageService _messageService;
    private readonly ILogger<OrderDetailModel> _logger;

    public OrderDetailModel(
        IOrderService orderService,
        IReturnRequestService returnRequestService,
        IProductReviewService reviewService,
        IReviewModerationService moderationService,
        ISellerRatingService sellerRatingService,
        IOrderMessageService messageService,
        ILogger<OrderDetailModel> logger)
    {
        _orderService = orderService;
        _returnRequestService = returnRequestService;
        _reviewService = reviewService;
        _moderationService = moderationService;
        _sellerRatingService = sellerRatingService;
        _messageService = messageService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the order being viewed.
    /// </summary>
    public Order? Order { get; set; }

    /// <summary>
    /// Gets a value indicating whether the order has been refunded or partially refunded.
    /// </summary>
    public bool HasRefunds => Order != null && (Order.RefundedAmount > 0 || Order.SubOrders.Any(so => so.RefundedAmount > 0));

    /// <summary>
    /// Gets a value indicating whether the order has any cancelled or refunded sub-orders.
    /// </summary>
    public bool HasCancellations => Order != null && Order.SubOrders.Any(so => so.Status == OrderStatus.Cancelled || so.Status == OrderStatus.Refunded);

    /// <summary>
    /// Gets or sets the return requests for this order's sub-orders.
    /// </summary>
    public Dictionary<int, List<ReturnRequest>> ReturnRequestsBySubOrder { get; set; } = new Dictionary<int, List<ReturnRequest>>();

    /// <summary>
    /// Gets or sets the reviews that the user has already submitted for order items.
    /// Key is OrderItemId, value is the review.
    /// </summary>
    public Dictionary<int, ProductReview> ExistingReviews { get; set; } = new Dictionary<int, ProductReview>();

    /// <summary>
    /// Gets or sets the seller ratings that the user has already submitted for sub-orders.
    /// Key is SellerSubOrderId, value is the rating.
    /// </summary>
    public Dictionary<int, SellerRating> ExistingSellerRatings { get; set; } = new Dictionary<int, SellerRating>();

    /// <summary>
    /// Gets or sets the order messages.
    /// </summary>
    public List<OrderMessage> Messages { get; set; } = new();

    [BindProperty]
    [Required(ErrorMessage = "Please enter a message.")]
    [MaxLength(2000, ErrorMessage = "Message cannot exceed 2000 characters.")]
    public string MessageInput { get; set; } = string.Empty;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET request to display order details.
    /// </summary>
    /// <param name="orderId">The order ID to display.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGetAsync(int orderId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid");
            return RedirectToPage("/Account/Login");
        }

        // Get order with authorization check
        Order = await _orderService.GetOrderByIdForBuyerAsync(orderId, userId);

        if (Order == null)
        {
            _logger.LogWarning("Order {OrderId} not found or user {UserId} not authorized to view it", orderId, userId);
            TempData["ErrorMessage"] = "Order not found or you don't have permission to view it.";
            return RedirectToPage("/Account/Orders");
        }

        // Load return requests for all sub-orders
        foreach (var subOrder in Order.SubOrders)
        {
            var returns = await _returnRequestService.GetReturnRequestsBySubOrderAsync(subOrder.Id);
            if (returns.Any())
            {
                ReturnRequestsBySubOrder[subOrder.Id] = returns;
            }

            // Load existing reviews for order items
            foreach (var item in subOrder.Items)
            {
                var hasReview = await _reviewService.HasUserReviewedOrderItemAsync(userId, item.Id);
                if (hasReview)
                {
                    // Note: We only need to know if a review exists for now
                    // Could enhance to load the actual review if needed for display
                    ExistingReviews[item.Id] = new ProductReview { OrderItemId = item.Id };
                }
            }

            // Load existing seller rating for this sub-order
            var hasSellerRating = await _sellerRatingService.HasUserRatedSubOrderAsync(userId, subOrder.Id);
            if (hasSellerRating)
            {
                ExistingSellerRatings[subOrder.Id] = new SellerRating { SellerSubOrderId = subOrder.Id };
            }
        }

        // Load order messages
        Messages = await _messageService.GetOrderMessagesAsync(orderId);
        
        // Mark messages as read
        await _messageService.MarkMessagesAsReadAsync(orderId, userId, false);

        return Page();
    }

    /// <summary>
    /// Handles POST request to send a message about the order.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostSendMessageAsync(int orderId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Account/Login");
        }

        if (!ModelState.IsValid)
        {
            await OnGetAsync(orderId);
            return Page();
        }

        try
        {
            await _messageService.SendMessageAsync(orderId, userId, MessageInput, false);
            SuccessMessage = "Your message has been sent to the seller.";
            return RedirectToPage(new { orderId });
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "You are not authorized to send messages for this order.";
            await OnGetAsync(orderId);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message for order {OrderId}", orderId);
            ErrorMessage = "Failed to send message. Please try again.";
            await OnGetAsync(orderId);
            return Page();
        }
    }

    /// <summary>
    /// Handles POST request to initiate a return for a sub-order.
    /// </summary>
    /// <param name="subOrderId">The sub-order ID to return.</param>
    /// <param name="orderId">The parent order ID for redirect.</param>
    /// <param name="requestType">The type of request (return or complaint).</param>
    /// <param name="reason">The return reason.</param>
    /// <param name="description">Optional description from buyer.</param>
    /// <param name="isFullReturn">Whether to return all items.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostInitiateReturnAsync(
        int subOrderId,
        int orderId,
        ReturnRequestType requestType,
        ReturnReason reason, 
        string? description, 
        bool isFullReturn = true)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid");
            return RedirectToPage("/Account/Login");
        }

        // Validate request type - must be one of the defined enum values
        if (requestType != ReturnRequestType.Return && requestType != ReturnRequestType.Complaint)
        {
            _logger.LogWarning("Invalid request type: {RequestType}", requestType);
            TempData["ErrorMessage"] = "Invalid request type. Please select either Return or Complaint.";
            return RedirectToPage(new { orderId });
        }

        // Validate reason - must be one of the defined enum values
        if (reason != ReturnReason.Damaged && 
            reason != ReturnReason.WrongItem && 
            reason != ReturnReason.NotAsDescribed && 
            reason != ReturnReason.ChangedMind && 
            reason != ReturnReason.ArrivedLate && 
            reason != ReturnReason.Other)
        {
            _logger.LogWarning("Invalid return reason: {Reason}", reason);
            TempData["ErrorMessage"] = "Invalid reason. Please select a valid reason from the list.";
            return RedirectToPage(new { orderId });
        }

        // Validate description length if provided
        if (!string.IsNullOrEmpty(description) && description.Length > 1000)
        {
            _logger.LogWarning("Description too long: {Length} characters", description.Length);
            TempData["ErrorMessage"] = "Description must not exceed 1000 characters.";
            return RedirectToPage(new { orderId });
        }

        try
        {
            // Create the return request
            var returnRequest = await _returnRequestService.CreateReturnRequestAsync(
                subOrderId,
                userId,
                requestType,
                reason,
                description,
                isFullReturn);

            var requestTypeLabel = requestType == ReturnRequestType.Complaint ? "Complaint" : "Return";
            TempData["SuccessMessage"] = $"{requestTypeLabel} request {returnRequest.ReturnNumber} has been submitted successfully. The seller will review it shortly.";
            
            // Redirect back to order detail to show the return status
            return RedirectToPage(new { orderId });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create return request for sub-order {SubOrderId}", subOrderId);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage(new { orderId });
        }
    }

    /// <summary>
    /// Handles POST request to submit a product review.
    /// </summary>
    /// <param name="orderItemId">The order item ID being reviewed.</param>
    /// <param name="orderId">The parent order ID for redirect.</param>
    /// <param name="rating">The rating (1-5 stars).</param>
    /// <param name="reviewText">Optional review text.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostSubmitReviewAsync(
        int orderItemId,
        int orderId,
        int rating,
        string? reviewText)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid");
            return RedirectToPage("/Account/Login");
        }

        // Validate rating
        if (rating < 1 || rating > 5)
        {
            TempData["ErrorMessage"] = "Rating must be between 1 and 5 stars.";
            return RedirectToPage(new { orderId });
        }

        // Validate review text length
        if (!string.IsNullOrEmpty(reviewText) && reviewText.Length > 2000)
        {
            TempData["ErrorMessage"] = "Review text must not exceed 2000 characters.";
            return RedirectToPage(new { orderId });
        }

        try
        {
            var review = await _reviewService.SubmitReviewAsync(userId, orderItemId, rating, reviewText);
            
            // Automatically check the review for potential issues
            try
            {
                await _moderationService.AutoCheckReviewAsync(review.Id);
            }
            catch (Exception autoCheckEx)
            {
                // Log but don't fail the submission if auto-check fails
                _logger.LogError(autoCheckEx, "Auto-check failed for review {ReviewId}", review.Id);
            }
            
            TempData["SuccessMessage"] = "Thank you for your review! It has been submitted successfully.";
            return RedirectToPage(new { orderId });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to submit review for order item {OrderItemId}", orderItemId);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage(new { orderId });
        }
    }

    /// <summary>
    /// Handles POST request to submit a seller rating.
    /// </summary>
    /// <param name="sellerSubOrderId">The seller sub-order ID being rated.</param>
    /// <param name="orderId">The parent order ID for redirect.</param>
    /// <param name="rating">The rating (1-5 stars).</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnPostSubmitSellerRatingAsync(
        int sellerSubOrderId,
        int orderId,
        int rating)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid");
            return RedirectToPage("/Account/Login");
        }

        // Validate rating
        if (rating < 1 || rating > 5)
        {
            TempData["ErrorMessage"] = "Rating must be between 1 and 5 stars.";
            return RedirectToPage(new { orderId });
        }

        try
        {
            var sellerRating = await _sellerRatingService.SubmitRatingAsync(userId, sellerSubOrderId, rating);
            TempData["SuccessMessage"] = "Thank you for rating the seller!";
            return RedirectToPage(new { orderId });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to submit seller rating for sub-order {SubOrderId}", sellerSubOrderId);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage(new { orderId });
        }
    }
}
