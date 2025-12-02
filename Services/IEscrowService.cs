using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for escrow management service.
/// Handles marketplace escrow for multi-vendor transactions.
/// </summary>
public interface IEscrowService
{
    /// <summary>
    /// Creates escrow allocations for each seller from a completed payment.
    /// Splits the payment amount across all seller sub-orders and calculates commissions.
    /// </summary>
    /// <param name="paymentTransactionId">The payment transaction ID.</param>
    /// <returns>A list of created escrow transactions.</returns>
    Task<List<EscrowTransaction>> CreateEscrowAllocationsAsync(int paymentTransactionId);

    /// <summary>
    /// Releases escrow funds to a seller when their sub-order is eligible for payout.
    /// </summary>
    /// <param name="escrowTransactionId">The escrow transaction ID.</param>
    /// <returns>True if release was successful, false otherwise.</returns>
    Task<bool> ReleaseEscrowAsync(int escrowTransactionId);

    /// <summary>
    /// Returns escrow funds to the buyer when an order is cancelled or refunded.
    /// </summary>
    /// <param name="escrowTransactionId">The escrow transaction ID.</param>
    /// <param name="refundAmount">The amount to refund (partial or full).</param>
    /// <param name="notes">Optional notes about the refund.</param>
    /// <returns>True if return was successful, false otherwise.</returns>
    Task<bool> ReturnEscrowToBuyerAsync(int escrowTransactionId, decimal refundAmount, string? notes = null);

    /// <summary>
    /// Marks escrow as eligible for payout based on fulfillment status.
    /// Calculates eligibility date based on marketplace policy.
    /// </summary>
    /// <param name="sellerSubOrderId">The seller sub-order ID.</param>
    /// <param name="daysUntilEligible">Number of days until funds are eligible for payout.</param>
    /// <returns>True if update was successful, false otherwise.</returns>
    Task<bool> MarkEscrowEligibleForPayoutAsync(int sellerSubOrderId, int daysUntilEligible = 7);

    /// <summary>
    /// Gets all escrow transactions for a specific payment.
    /// </summary>
    /// <param name="paymentTransactionId">The payment transaction ID.</param>
    /// <returns>A list of escrow transactions.</returns>
    Task<List<EscrowTransaction>> GetEscrowTransactionsByPaymentAsync(int paymentTransactionId);

    /// <summary>
    /// Gets all escrow transactions for a seller's store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="status">Optional filter by escrow status.</param>
    /// <returns>A list of escrow transactions.</returns>
    Task<List<EscrowTransaction>> GetEscrowTransactionsByStoreAsync(int storeId, EscrowStatus? status = null);

    /// <summary>
    /// Gets the escrow transaction for a specific seller sub-order.
    /// </summary>
    /// <param name="sellerSubOrderId">The seller sub-order ID.</param>
    /// <returns>The escrow transaction, or null if not found.</returns>
    Task<EscrowTransaction?> GetEscrowTransactionBySubOrderAsync(int sellerSubOrderId);

    /// <summary>
    /// Processes automatic escrow releases for all eligible transactions.
    /// Should be called periodically (e.g., daily background job).
    /// </summary>
    /// <returns>The number of escrow transactions released.</returns>
    Task<int> ProcessEligiblePayoutsAsync();
}
