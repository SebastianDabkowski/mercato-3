using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for refund management service.
/// Handles full and partial refunds with balance and commission adjustments.
/// </summary>
public interface IRefundService
{
    /// <summary>
    /// Processes a full refund for an entire order.
    /// Returns all funds to the buyer, reverses all escrow allocations, and adjusts commissions.
    /// </summary>
    /// <param name="orderId">The order ID to refund.</param>
    /// <param name="reason">The reason for the refund.</param>
    /// <param name="initiatedByUserId">The user ID who initiated the refund (admin or seller).</param>
    /// <param name="notes">Optional notes about the refund.</param>
    /// <returns>The created refund transaction.</returns>
    Task<RefundTransaction> ProcessFullRefundAsync(
        int orderId,
        string reason,
        int initiatedByUserId,
        string? notes = null);

    /// <summary>
    /// Processes a partial refund for a seller sub-order or specific amount.
    /// Adjusts escrow, commission, and seller balance proportionally.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <param name="sellerSubOrderId">The seller sub-order ID to refund (for partial seller refunds).</param>
    /// <param name="refundAmount">The amount to refund.</param>
    /// <param name="reason">The reason for the refund.</param>
    /// <param name="initiatedByUserId">The user ID who initiated the refund (admin or seller).</param>
    /// <param name="notes">Optional notes about the refund.</param>
    /// <param name="returnRequestId">Optional return request ID if this refund is linked to a return/complaint case.</param>
    /// <returns>The created refund transaction.</returns>
    Task<RefundTransaction> ProcessPartialRefundAsync(
        int orderId,
        int sellerSubOrderId,
        decimal refundAmount,
        string reason,
        int initiatedByUserId,
        string? notes = null,
        int? returnRequestId = null);

    /// <summary>
    /// Validates whether a refund can be processed for the given order.
    /// Checks business rules, payment status, and available refundable amounts.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <param name="refundAmount">The requested refund amount (null for full refund validation).</param>
    /// <returns>A tuple indicating if refund is valid and an error message if not.</returns>
    Task<(bool IsValid, string? ErrorMessage)> ValidateRefundEligibilityAsync(
        int orderId,
        decimal? refundAmount = null);

    /// <summary>
    /// Validates whether a partial refund can be processed for a seller sub-order.
    /// Prevents negative balances and validates business rules.
    /// </summary>
    /// <param name="sellerSubOrderId">The seller sub-order ID.</param>
    /// <param name="refundAmount">The requested refund amount.</param>
    /// <returns>A tuple indicating if refund is valid and an error message if not.</returns>
    Task<(bool IsValid, string? ErrorMessage)> ValidatePartialRefundEligibilityAsync(
        int sellerSubOrderId,
        decimal refundAmount);

    /// <summary>
    /// Gets all refund transactions for an order.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <returns>A list of refund transactions.</returns>
    Task<List<RefundTransaction>> GetRefundsByOrderAsync(int orderId);

    /// <summary>
    /// Gets all refund transactions in the system.
    /// </summary>
    /// <param name="status">Optional filter by refund status.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <returns>A list of refund transactions.</returns>
    Task<List<RefundTransaction>> GetAllRefundsAsync(
        RefundStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Gets a refund transaction by ID.
    /// </summary>
    /// <param name="refundId">The refund transaction ID.</param>
    /// <returns>The refund transaction, or null if not found.</returns>
    Task<RefundTransaction?> GetRefundByIdAsync(int refundId);

    /// <summary>
    /// Gets the total refunded amount for an order.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <returns>The total amount refunded.</returns>
    Task<decimal> GetTotalRefundedAmountAsync(int orderId);

    /// <summary>
    /// Gets all refund transactions initiated by a seller for their orders.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="status">Optional filter by refund status.</param>
    /// <returns>A list of refund transactions.</returns>
    Task<List<RefundTransaction>> GetRefundsByStoreAsync(int storeId, RefundStatus? status = null);

    /// <summary>
    /// Retries a failed refund transaction.
    /// Attempts to reprocess the refund with the payment provider.
    /// </summary>
    /// <param name="refundId">The refund transaction ID.</param>
    /// <returns>The updated refund transaction.</returns>
    Task<RefundTransaction> RetryFailedRefundAsync(int refundId);
}
