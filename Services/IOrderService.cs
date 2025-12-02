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
    /// <param name="guestEmail">The guest email (for guest checkout).</param>
    /// <returns>The created order.</returns>
    Task<Order> CreateOrderFromCartAsync(int? userId, string? sessionId, int addressId, string? guestEmail);

    /// <summary>
    /// Gets an order by its ID.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <returns>The order, or null if not found.</returns>
    Task<Order?> GetOrderByIdAsync(int orderId);

    /// <summary>
    /// Gets all orders for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of orders.</returns>
    Task<List<Order>> GetUserOrdersAsync(int userId);

    /// <summary>
    /// Validates if items in the cart can be shipped to the delivery address.
    /// </summary>
    /// <param name="userId">The user ID (null for guest).</param>
    /// <param name="sessionId">The session ID (for guest).</param>
    /// <param name="countryCode">The country code of the delivery address.</param>
    /// <returns>A validation result indicating if shipping is allowed.</returns>
    Task<(bool IsValid, string? ErrorMessage)> ValidateShippingForCartAsync(int? userId, string? sessionId, string countryCode);
}
