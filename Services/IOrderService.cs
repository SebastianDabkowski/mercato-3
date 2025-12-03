using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for order management service.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Creates an order from the current cart.
    /// </summary>
    /// <param name="userId">The user ID (null for guest checkout).</param>
    /// <param name="sessionId">The session ID (for guest checkout).</param>
    /// <param name="addressId">The delivery address ID.</param>
    /// <param name="selectedShippingMethods">Dictionary of store ID to shipping method ID.</param>
    /// <param name="paymentMethodId">The selected payment method ID.</param>
    /// <param name="guestEmail">The guest email (for guest checkout).</param>
    /// <returns>The created order.</returns>
    Task<Order> CreateOrderFromCartAsync(
        int? userId, 
        string? sessionId, 
        int addressId, 
        Dictionary<int, int> selectedShippingMethods,
        int paymentMethodId,
        string? guestEmail);

    /// <summary>
    /// Gets an order by its ID.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <returns>The order, or null if not found.</returns>
    Task<Order?> GetOrderByIdAsync(int orderId);

    /// <summary>
    /// Gets an order by its ID with full details for buyer view, including authorization check.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <param name="userId">The user ID of the buyer requesting the order.</param>
    /// <returns>The order with full details, or null if not found or not authorized.</returns>
    Task<Order?> GetOrderByIdForBuyerAsync(int orderId, int userId);

    /// <summary>
    /// Gets all orders for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of orders.</returns>
    Task<List<Order>> GetUserOrdersAsync(int userId);

    /// <summary>
    /// Gets filtered and paginated orders for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="statuses">Filter by order statuses (optional).</param>
    /// <param name="fromDate">Filter by minimum order date (optional).</param>
    /// <param name="toDate">Filter by maximum order date (optional).</param>
    /// <param name="sellerId">Filter by seller/store ID (optional).</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of orders per page.</param>
    /// <returns>A tuple containing the list of orders and total count.</returns>
    Task<(List<Order> Orders, int TotalCount)> GetUserOrdersFilteredAsync(
        int userId, 
        List<OrderStatus>? statuses = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? sellerId = null,
        int page = 1,
        int pageSize = 10);

    /// <summary>
    /// Gets unique sellers from a user's orders for filter dropdown.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of stores that have fulfilled orders for this user.</returns>
    Task<List<Store>> GetUserOrderSellersAsync(int userId);

    /// <summary>
    /// Validates if items in the cart can be shipped to the delivery address.
    /// </summary>
    /// <param name="userId">The user ID (null for guest).</param>
    /// <param name="sessionId">The session ID (for guest).</param>
    /// <param name="countryCode">The country code of the delivery address.</param>
    /// <returns>A validation result indicating if shipping is allowed.</returns>
    Task<(bool IsValid, string? ErrorMessage)> ValidateShippingForCartAsync(int? userId, string? sessionId, string countryCode);

    /// <summary>
    /// Validates stock availability and prices for items in the cart before placing an order.
    /// </summary>
    /// <param name="userId">The user ID (null for guest).</param>
    /// <param name="sessionId">The session ID (for guest).</param>
    /// <returns>An order validation result containing any stock or price issues.</returns>
    Task<OrderValidationResult> ValidateCartForOrderAsync(int? userId, string? sessionId);

    /// <summary>
    /// Gets all seller sub-orders for a parent order.
    /// </summary>
    /// <param name="parentOrderId">The parent order ID.</param>
    /// <returns>A list of seller sub-orders.</returns>
    Task<List<SellerSubOrder>> GetSubOrdersByParentOrderIdAsync(int parentOrderId);

    /// <summary>
    /// Gets all seller sub-orders for a specific store/seller.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>A list of seller sub-orders for the store.</returns>
    Task<List<SellerSubOrder>> GetSubOrdersByStoreIdAsync(int storeId);

    /// <summary>
    /// Gets a specific seller sub-order by its ID.
    /// </summary>
    /// <param name="subOrderId">The sub-order ID.</param>
    /// <returns>The seller sub-order, or null if not found.</returns>
    Task<SellerSubOrder?> GetSubOrderByIdAsync(int subOrderId);

    /// <summary>
    /// Gets a specific seller sub-order by its ID with authorization check.
    /// </summary>
    /// <param name="subOrderId">The sub-order ID.</param>
    /// <param name="sellerUserId">The seller user ID requesting the sub-order.</param>
    /// <returns>The seller sub-order, or null if not found or not authorized.</returns>
    Task<SellerSubOrder?> GetSubOrderByIdForSellerAsync(int subOrderId, int sellerUserId);

    /// <summary>
    /// Gets filtered and paginated seller sub-orders for a specific store/seller.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="statuses">Filter by order statuses (optional).</param>
    /// <param name="fromDate">Filter by minimum order date (optional).</param>
    /// <param name="toDate">Filter by maximum order date (optional).</param>
    /// <param name="buyerEmail">Filter by buyer email (partial match, optional).</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of sub-orders per page.</param>
    /// <returns>A tuple containing the list of seller sub-orders and total count.</returns>
    Task<(List<SellerSubOrder> SubOrders, int TotalCount)> GetSubOrdersFilteredAsync(
        int storeId,
        List<OrderStatus>? statuses = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? buyerEmail = null,
        int page = 1,
        int pageSize = 10);
}
