namespace MercatoApp.Models;

/// <summary>
/// Represents the type of analytics event being tracked.
/// These events support Phase 2 advanced analytics dashboards.
/// </summary>
public enum AnalyticsEventType
{
    /// <summary>
    /// User performed a search query.
    /// </summary>
    Search,

    /// <summary>
    /// User viewed a product detail page.
    /// </summary>
    ProductView,

    /// <summary>
    /// User added an item to their shopping cart.
    /// </summary>
    AddToCart,

    /// <summary>
    /// User removed an item from their shopping cart.
    /// </summary>
    RemoveFromCart,

    /// <summary>
    /// User initiated the checkout process.
    /// </summary>
    CheckoutStart,

    /// <summary>
    /// User completed an order successfully.
    /// </summary>
    OrderComplete,

    /// <summary>
    /// User viewed their cart.
    /// </summary>
    CartView,

    /// <summary>
    /// User viewed a category page.
    /// </summary>
    CategoryView,

    /// <summary>
    /// User clicked on a product from search or listing.
    /// </summary>
    ProductClick,

    /// <summary>
    /// User applied a promo code.
    /// </summary>
    PromoCodeApplied,

    /// <summary>
    /// User initiated a return or complaint.
    /// </summary>
    ReturnInitiated,

    /// <summary>
    /// User left a product review.
    /// </summary>
    ReviewSubmitted
}
