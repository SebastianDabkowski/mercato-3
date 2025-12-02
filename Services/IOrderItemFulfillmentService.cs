using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for order item fulfillment service.
/// Handles item-level operations for partial fulfillment within sub-orders.
/// </summary>
public interface IOrderItemFulfillmentService
{
    /// <summary>
    /// Ships a specific quantity of an order item.
    /// Supports partial shipping where only some items are shipped while others remain pending.
    /// </summary>
    /// <param name="orderItemId">The order item ID.</param>
    /// <param name="quantityToShip">The quantity to ship (must be <= available quantity).</param>
    /// <param name="userId">The user ID making the change (for audit trail).</param>
    /// <returns>Success status and error message if applicable.</returns>
    Task<(bool Success, string? ErrorMessage)> ShipItemQuantityAsync(
        int orderItemId,
        int quantityToShip,
        int? userId = null);

    /// <summary>
    /// Cancels a specific quantity of an order item.
    /// Supports partial cancellation where only some items are cancelled while others proceed.
    /// </summary>
    /// <param name="orderItemId">The order item ID.</param>
    /// <param name="quantityToCancel">The quantity to cancel (must be <= available quantity).</param>
    /// <param name="userId">The user ID making the change (for audit trail).</param>
    /// <returns>Success status, error message, and refund amount calculated for cancelled quantity.</returns>
    Task<(bool Success, string? ErrorMessage, decimal RefundAmount)> CancelItemQuantityAsync(
        int orderItemId,
        int quantityToCancel,
        int? userId = null);

    /// <summary>
    /// Marks an order item as preparing.
    /// Can only transition from New status.
    /// </summary>
    /// <param name="orderItemId">The order item ID.</param>
    /// <param name="userId">The user ID making the change (for audit trail).</param>
    /// <returns>Success status and error message if applicable.</returns>
    Task<(bool Success, string? ErrorMessage)> MarkItemAsPreparingAsync(
        int orderItemId,
        int? userId = null);

    /// <summary>
    /// Gets the available quantity for an order item (total - shipped - cancelled).
    /// </summary>
    /// <param name="orderItemId">The order item ID.</param>
    /// <returns>The available quantity that can be shipped or cancelled.</returns>
    Task<int> GetAvailableQuantityAsync(int orderItemId);

    /// <summary>
    /// Validates if item-level fulfillment actions are allowed for a sub-order.
    /// Checks sub-order status and payment status.
    /// </summary>
    /// <param name="subOrderId">The sub-order ID.</param>
    /// <returns>True if item-level actions are allowed, false otherwise with error message.</returns>
    Task<(bool IsAllowed, string? ErrorMessage)> ValidateItemFulfillmentAsync(int subOrderId);

    /// <summary>
    /// Calculates the refund amount for a cancelled item quantity.
    /// Includes proportional item cost and tax.
    /// </summary>
    /// <param name="orderItemId">The order item ID.</param>
    /// <param name="quantityToCancel">The quantity being cancelled.</param>
    /// <returns>The refund amount for the cancelled quantity.</returns>
    Task<decimal> CalculateItemRefundAmountAsync(int orderItemId, int quantityToCancel);

    /// <summary>
    /// Gets all order items for a sub-order with their fulfillment status.
    /// </summary>
    /// <param name="subOrderId">The sub-order ID.</param>
    /// <returns>List of order items with fulfillment tracking data.</returns>
    Task<List<OrderItem>> GetSubOrderItemsWithStatusAsync(int subOrderId);
}
