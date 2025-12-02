using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Result of a payout operation.
/// </summary>
public class PayoutResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the list of errors that occurred.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the payout that was created or processed.
    /// </summary>
    public Payout? Payout { get; set; }
}

/// <summary>
/// Summary of eligible balance for payout.
/// </summary>
public class PayoutBalanceSummary
{
    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the total eligible balance amount.
    /// </summary>
    public decimal EligibleBalance { get; set; }

    /// <summary>
    /// Gets or sets the number of escrow transactions eligible for payout.
    /// </summary>
    public int EligibleTransactionCount { get; set; }

    /// <summary>
    /// Gets or sets whether the balance meets the minimum threshold.
    /// </summary>
    public bool MeetsThreshold { get; set; }

    /// <summary>
    /// Gets or sets the minimum threshold amount.
    /// </summary>
    public decimal MinimumThreshold { get; set; }
}

/// <summary>
/// Interface for payout management service.
/// Handles seller payout scheduling, processing, and tracking.
/// </summary>
public interface IPayoutService
{
    /// <summary>
    /// Creates or updates a payout schedule for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="frequency">The payout frequency.</param>
    /// <param name="minimumThreshold">The minimum payout threshold.</param>
    /// <param name="dayOfWeek">The day of week for weekly payouts (0-6).</param>
    /// <param name="dayOfMonth">The day of month for monthly payouts (1-28).</param>
    /// <returns>The created or updated payout schedule.</returns>
    Task<PayoutSchedule> CreateOrUpdatePayoutScheduleAsync(
        int storeId,
        PayoutFrequency frequency,
        decimal minimumThreshold,
        int? dayOfWeek = null,
        int? dayOfMonth = null);

    /// <summary>
    /// Gets the payout schedule for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>The payout schedule, or null if not configured.</returns>
    Task<PayoutSchedule?> GetPayoutScheduleAsync(int storeId);

    /// <summary>
    /// Gets the eligible balance summary for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>The balance summary.</returns>
    Task<PayoutBalanceSummary> GetEligibleBalanceSummaryAsync(int storeId);

    /// <summary>
    /// Creates a payout for eligible escrow balances.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="scheduledDate">The scheduled date for the payout.</param>
    /// <returns>The result of the payout creation.</returns>
    Task<PayoutResult> CreatePayoutAsync(int storeId, DateTime scheduledDate);

    /// <summary>
    /// Processes scheduled payouts that are due.
    /// Should be called by a background job.
    /// </summary>
    /// <returns>The number of payouts processed.</returns>
    Task<int> ProcessScheduledPayoutsAsync();

    /// <summary>
    /// Processes a specific payout.
    /// </summary>
    /// <param name="payoutId">The payout ID.</param>
    /// <returns>The result of the payout processing.</returns>
    Task<PayoutResult> ProcessPayoutAsync(int payoutId);

    /// <summary>
    /// Retries failed payouts that are due for retry.
    /// Should be called by a background job.
    /// </summary>
    /// <returns>The number of payouts retried.</returns>
    Task<int> RetryFailedPayoutsAsync();

    /// <summary>
    /// Gets all payouts for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="status">Optional filter by status.</param>
    /// <returns>A list of payouts.</returns>
    Task<List<Payout>> GetPayoutsAsync(int storeId, PayoutStatus? status = null);

    /// <summary>
    /// Gets a specific payout by ID.
    /// </summary>
    /// <param name="payoutId">The payout ID.</param>
    /// <returns>The payout, or null if not found.</returns>
    Task<Payout?> GetPayoutAsync(int payoutId);

    /// <summary>
    /// Generates payouts for all stores with schedules due for processing.
    /// Should be called by a background job.
    /// </summary>
    /// <returns>The number of payouts created.</returns>
    Task<int> GenerateScheduledPayoutsAsync();
}
