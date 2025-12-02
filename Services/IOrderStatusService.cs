using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for order status management service.
/// Handles order status transitions with validation and business logic.
/// </summary>
public interface IOrderStatusService
{
    /// <summary>
    /// Updates the status of an order when payment is authorized.
    /// Sets order and all sub-orders to 'Paid' status.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> MarkOrderAsPaidAsync(int orderId);

    /// <summary>
    /// Updates a sub-order status to 'Preparing'.
    /// Can only transition from 'Paid' status.
    /// </summary>
    /// <param name="subOrderId">The sub-order ID.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<(bool Success, string? ErrorMessage)> UpdateSubOrderToPreparingAsync(int subOrderId);

    /// <summary>
    /// Updates a sub-order status to 'Shipped' with optional tracking information.
    /// Can only transition from 'Preparing' status.
    /// </summary>
    /// <param name="subOrderId">The sub-order ID.</param>
    /// <param name="trackingNumber">Optional tracking number.</param>
    /// <param name="carrierName">Optional carrier/courier name.</param>
    /// <param name="trackingUrl">Optional tracking URL.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<(bool Success, string? ErrorMessage)> UpdateSubOrderToShippedAsync(
        int subOrderId, 
        string? trackingNumber = null, 
        string? carrierName = null,
        string? trackingUrl = null);

    /// <summary>
    /// Updates a sub-order status to 'Delivered'.
    /// Can only transition from 'Shipped' status.
    /// </summary>
    /// <param name="subOrderId">The sub-order ID.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<(bool Success, string? ErrorMessage)> UpdateSubOrderToDeliveredAsync(int subOrderId);

    /// <summary>
    /// Cancels an order or sub-order.
    /// Can only cancel orders that haven't been shipped yet.
    /// </summary>
    /// <param name="subOrderId">The sub-order ID.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<(bool Success, string? ErrorMessage)> CancelSubOrderAsync(int subOrderId);

    /// <summary>
    /// Marks a sub-order as refunded.
    /// </summary>
    /// <param name="subOrderId">The sub-order ID.</param>
    /// <param name="refundAmount">The amount being refunded.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<(bool Success, string? ErrorMessage)> RefundSubOrderAsync(int subOrderId, decimal refundAmount);

    /// <summary>
    /// Validates if a status transition is allowed based on current status.
    /// </summary>
    /// <param name="currentStatus">The current status.</param>
    /// <param name="newStatus">The desired new status.</param>
    /// <returns>True if the transition is valid, false otherwise.</returns>
    bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus);

    /// <summary>
    /// Updates the parent order status based on sub-order statuses.
    /// The parent order status reflects the overall state of all sub-orders.
    /// </summary>
    /// <param name="orderId">The parent order ID.</param>
    /// <returns>Task representing the async operation.</returns>
    Task UpdateParentOrderStatusAsync(int orderId);
}
