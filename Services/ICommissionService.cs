using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for commission calculation and management service.
/// </summary>
public interface ICommissionService
{
    /// <summary>
    /// Calculates commission for a given amount with per-seller and per-category overrides.
    /// </summary>
    /// <param name="grossAmount">The gross amount to calculate commission from.</param>
    /// <param name="storeId">The store ID for seller-specific commission rules.</param>
    /// <param name="categoryId">Optional category ID for category-specific commission rules.</param>
    /// <returns>A tuple containing the commission amount, percentage, fixed amount, and source.</returns>
    Task<(decimal CommissionAmount, decimal Percentage, decimal FixedAmount, string Source, int? CategoryId)> CalculateCommissionAsync(
        decimal grossAmount,
        int storeId,
        int? categoryId = null);

    /// <summary>
    /// Records a commission transaction for audit trail.
    /// </summary>
    /// <param name="escrowTransactionId">The escrow transaction ID.</param>
    /// <param name="storeId">The store ID.</param>
    /// <param name="categoryId">Optional category ID.</param>
    /// <param name="transactionType">The transaction type (Initial or RefundAdjustment).</param>
    /// <param name="grossAmount">The gross amount.</param>
    /// <param name="commissionAmount">The calculated commission amount.</param>
    /// <param name="percentage">The commission percentage applied.</param>
    /// <param name="fixedAmount">The fixed commission amount applied.</param>
    /// <param name="source">The commission source (Global, Seller, or Category).</param>
    /// <param name="notes">Optional notes.</param>
    /// <returns>The created commission transaction.</returns>
    Task<CommissionTransaction> RecordCommissionTransactionAsync(
        int escrowTransactionId,
        int storeId,
        int? categoryId,
        string transactionType,
        decimal grossAmount,
        decimal commissionAmount,
        decimal percentage,
        decimal fixedAmount,
        string source,
        string? notes = null);

    /// <summary>
    /// Gets all commission transactions for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <returns>A list of commission transactions.</returns>
    Task<List<CommissionTransaction>> GetCommissionTransactionsByStoreAsync(
        int storeId,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Gets all commission transactions for an escrow transaction.
    /// </summary>
    /// <param name="escrowTransactionId">The escrow transaction ID.</param>
    /// <returns>A list of commission transactions.</returns>
    Task<List<CommissionTransaction>> GetCommissionTransactionsByEscrowAsync(int escrowTransactionId);

    /// <summary>
    /// Gets the total commission earned by the platform for a date range.
    /// </summary>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <returns>The total commission amount.</returns>
    Task<decimal> GetTotalCommissionAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Recalculates commission for a partial refund.
    /// Creates a refund adjustment commission transaction.
    /// </summary>
    /// <param name="escrowTransactionId">The escrow transaction ID.</param>
    /// <param name="refundAmount">The refund amount.</param>
    /// <param name="originalCommissionAmount">The original commission amount.</param>
    /// <returns>The adjusted commission amount (negative value for refund).</returns>
    Task<decimal> RecalculateCommissionForRefundAsync(
        int escrowTransactionId,
        decimal refundAmount,
        decimal originalCommissionAmount);
}
