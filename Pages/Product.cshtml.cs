using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MercatoApp.Pages;

public class ProductModel : PageModel
{
    private readonly IProductService _productService;
    private readonly IProductVariantService _variantService;
    private readonly IRecentlyViewedService _recentlyViewedService;
    private readonly ICartService _cartService;
    private readonly IGuestCartService _guestCartService;
    private readonly IProductReviewService _reviewService;
    private readonly IProductQuestionService _questionService;
    private readonly IAnalyticsEventService _analyticsService;
    private readonly ILogger<ProductModel> _logger;

    public ProductModel(
        IProductService productService,
        IProductVariantService variantService,
        IRecentlyViewedService recentlyViewedService,
        ICartService cartService,
        IGuestCartService guestCartService,
        IProductReviewService reviewService,
        IProductQuestionService questionService,
        IAnalyticsEventService analyticsService,
        ILogger<ProductModel> logger)
    {
        _productService = productService;
        _variantService = variantService;
        _recentlyViewedService = recentlyViewedService;
        _cartService = cartService;
        _guestCartService = guestCartService;
        _reviewService = reviewService;
        _questionService = questionService;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public Product? Product { get; set; }
    public List<ProductVariantAttribute> VariantAttributes { get; set; } = new();
    public List<ProductVariant> Variants { get; set; } = new();
    public ProductVariant? SelectedVariant { get; set; }
    public List<ProductReview> Reviews { get; set; } = new();
    public decimal? AverageRating { get; set; }
    public int TotalReviewCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => TotalReviewCount > 0 ? (int)Math.Ceiling((double)TotalReviewCount / PageSize) : 0;
    public ReviewSortOption SortOption { get; set; } = ReviewSortOption.Newest;
    public List<ProductQuestion> Questions { get; set; } = new();

    [BindProperty]
    [Required(ErrorMessage = "Please enter your question.")]
    [MaxLength(2000, ErrorMessage = "Question cannot exceed 2000 characters.")]
    public string QuestionInput { get; set; } = string.Empty;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets a value indicating whether the product is publicly viewable (Active status).
    /// </summary>
    public bool IsProductAvailable => Product != null && Product.Status == ProductStatus.Active;

    /// <summary>
    /// Gets the message to display when product is not available.
    /// </summary>
    public string? UnavailableMessage { get; private set; }

    /// <summary>
    /// Gets the referrer URL for "Back to results" navigation.
    /// </summary>
    public string? ReferrerUrl { get; private set; }

    /// <summary>
    /// Gets a value indicating whether there is a valid referrer URL.
    /// </summary>
    public bool HasReferrer => !string.IsNullOrEmpty(ReferrerUrl);

    public async Task<IActionResult> OnGetAsync(int id, int? variantId = null, ReviewSortOption sort = ReviewSortOption.Newest, int page = 1)
    {
        // Validate and set pagination parameters
        CurrentPage = page < 1 ? 1 : page;
        SortOption = sort;
        
        // Try to get the product - we want to handle the case where a product
        // exists but is not available (archived/inactive) vs doesn't exist at all
        Product = await _productService.GetProductByIdAsync(id);

        if (Product == null)
        {
            return NotFound();
        }

        // Capture referrer URL for "Back to results" navigation
        // Only use referrer if it's from the same origin (security consideration)
        var refererUri = Request.GetTypedHeaders().Referer;
        if (refererUri != null && 
            string.Equals(refererUri.Authority, Request.Host.Value, StringComparison.OrdinalIgnoreCase) &&
            (refererUri.AbsolutePath.StartsWith("/Search", StringComparison.OrdinalIgnoreCase) || 
             refererUri.AbsolutePath.StartsWith("/Category/", StringComparison.OrdinalIgnoreCase)))
        {
            ReferrerUrl = refererUri.ToString();
        }

        // Load variant data if the product has variants
        if (Product.HasVariants)
        {
            VariantAttributes = await _variantService.GetVariantAttributesAsync(id, null);
            Variants = await _variantService.GetVariantsAsync(id, null);

            // If a specific variant is selected, load it
            if (variantId.HasValue)
            {
                SelectedVariant = await _variantService.GetVariantByIdAsync(variantId.Value, null);
            }
        }

        // Handle products that are not publicly viewable
        if (!IsProductAvailable)
        {
            UnavailableMessage = Product.Status switch
            {
                ProductStatus.Archived => "This product is no longer available.",
                ProductStatus.Suspended => "This product is currently unavailable.",
                ProductStatus.Draft => "This product is not yet available for viewing.",
                _ => "This product is currently unavailable."
            };
        }
        else
        {
            // Track the product view only if the product is available
            _recentlyViewedService.TrackProductView(id);
            
            // Track analytics event (fire-and-forget)
            _ = TrackProductViewEventAsync(Product);
            
            // Load reviews for available products with pagination and sorting
            TotalReviewCount = await _reviewService.GetApprovedReviewCountAsync(id);
            Reviews = await _reviewService.GetApprovedReviewsForProductAsync(id, SortOption, CurrentPage, PageSize);
            AverageRating = await _reviewService.GetAverageRatingAsync(id);
            
            // Load product questions
            Questions = await _questionService.GetProductQuestionsAsync(id);
            
            // Mark replies as read if user is authenticated and has questions
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out var userId))
                {
                    foreach (var question in Questions.Where(q => q.BuyerId == userId && q.Replies.Any(r => !r.IsReadByBuyer)))
                    {
                        await _questionService.MarkRepliesAsReadAsync(question.Id, userId);
                    }
                }
            }
        }

        return Page();
    }

    private async Task TrackProductViewEventAsync(Product product)
    {
        try
        {
            int? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (int.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                }
            }

            var sessionId = HttpContext.Session.Id;

            await _analyticsService.TrackEventAsync(new AnalyticsEventData
            {
                EventType = AnalyticsEventType.ProductView,
                UserId = userId,
                SessionId = sessionId,
                ProductId = product.Id,
                CategoryId = product.CategoryId,
                StoreId = product.StoreId,
                Value = product.Price,
                UserAgent = Request.Headers.UserAgent.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Referrer = Request.Headers.Referer.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking product view event");
        }
    }

    public async Task<IActionResult> OnPostAskQuestionAsync(int id)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage("/Account/Login", new { returnUrl = $"/Product/{id}" });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            ErrorMessage = "Unable to identify user.";
            return RedirectToPage(new { id });
        }

        if (!ModelState.IsValid)
        {
            await OnGetAsync(id);
            return Page();
        }

        try
        {
            await _questionService.AskQuestionAsync(id, userId, QuestionInput);
            SuccessMessage = "Your question has been submitted. The seller will be notified.";
            return RedirectToPage(new { id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await OnGetAsync(id);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAddToCartAsync(int id, int? variantId, int quantity = 1)
    {
        // Reload product data
        await OnGetAsync(id, variantId);

        if (Product == null || !IsProductAvailable)
        {
            ErrorMessage = "This product is not available.";
            return Page();
        }

        // Validate stock
        var hasStock = false;
        if (Product.HasVariants && variantId.HasValue && SelectedVariant != null)
        {
            hasStock = SelectedVariant.Stock > 0 && SelectedVariant.IsEnabled;
        }
        else if (!Product.HasVariants)
        {
            hasStock = Product.Stock > 0;
        }

        if (!hasStock)
        {
            ErrorMessage = "This product is out of stock.";
            return Page();
        }

        try
        {
            var (userId, sessionId) = GetUserOrSessionId();
            await _cartService.AddToCartAsync(userId, sessionId, id, variantId, quantity);
            SuccessMessage = "Item added to cart successfully!";
            return RedirectToPage("/Cart");
        }
        catch (Exception)
        {
            ErrorMessage = "Failed to add item to cart. Please try again.";
            return Page();
        }
    }

    private (int? userId, string? sessionId) GetUserOrSessionId()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return (userId, null);
            }
        }

        // Use persistent guest cart ID for anonymous users
        var guestCartId = _guestCartService.GetOrCreateGuestCartId();
        return (null, guestCartId);
    }
}