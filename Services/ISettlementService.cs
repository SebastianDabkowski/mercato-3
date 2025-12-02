using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Result of a settlement operation.
/// </summary>
public class SettlementResult
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
    /// Gets or sets the settlement that was created or processed.
    /// </summary>
    public Settlement? Settlement { get; set; }
}

/// <summary>
/// Summary of a settlement period.
/// </summary>
public class SettlementSummary
{
    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the period start date.
    /// </summary>
    public DateTime PeriodStartDate { get; set; }

    /// <summary>
    /// Gets or sets the period end date.
    /// </summary>
    public DateTime PeriodEndDate { get; set; }

    /// <summary>
    /// Gets or sets the number of orders in the period.
    /// </summary>
    public int OrderCount { get; set; }

    /// <summary>
    /// Gets or sets the gross sales amount.
    /// </summary>
    public decimal GrossSales { get; set; }

    /// <summary>
    /// Gets or sets the total refunds.
    /// </summary>
    public decimal Refunds { get; set; }

    /// <summary>
    /// Gets or sets the total commission.
    /// </summary>
    public decimal Commission { get; set; }

    /// <summary>
    /// Gets or sets the net amount.
    /// </summary>
    public decimal NetAmount { get; set; }

    /// <summary>
    /// Gets or sets whether a settlement already exists for this period.
    /// </summary>
    public bool HasExistingSettlement { get; set; }

    /// <summary>
    /// Gets or sets the existing settlement ID if one exists.
    /// </summary>
    public int? ExistingSettlementId { get; set; }
}

/// <summary>
/// Interface for settlement report management service.
/// Handles monthly settlement generation, reporting, and export.
/// </summary>
public interface ISettlementService
{
    /// <summary>
    /// Generates a settlement report for a seller for a specific period.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="periodStartDate">The period start date.</param>
    /// <param name="periodEndDate">The period end date.</param>
    /// <returns>The result of the settlement generation.</returns>
    Task<SettlementResult> GenerateSettlementAsync(int storeId, DateTime periodStartDate, DateTime periodEndDate);

    /// <summary>
    /// Generates monthly settlements for all sellers for a specific month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <returns>The number of settlements generated.</returns>
    Task<int> GenerateMonthlySettlementsAsync(int year, int month);

    /// <summary>
    /// Regenerates a settlement, creating a new version and marking the old one as superseded.
    /// </summary>
    /// <param name="settlementId">The settlement ID to regenerate.</param>
    /// <returns>The result of the settlement regeneration.</returns>
    Task<SettlementResult> RegenerateSettlementAsync(int settlementId);

    /// <summary>
    /// Gets a settlement by ID.
    /// </summary>
    /// <param name="settlementId">The settlement ID.</param>
    /// <returns>The settlement, or null if not found.</returns>
    Task<Settlement?> GetSettlementAsync(int settlementId);

    /// <summary>
    /// Gets all settlements for a store.
    /// </summary>
    /// <param name="storeId">The store ID. Use 0 to get settlements for all stores.</param>
    /// <param name="includeSuperseded">Whether to include superseded versions.</param>
    /// <returns>A list of settlements.</returns>
    Task<List<Settlement>> GetSettlementsAsync(int storeId, bool includeSuperseded = false);

    /// <summary>
    /// Gets a summary of a potential settlement period without creating it.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="periodStartDate">The period start date.</param>
    /// <param name="periodEndDate">The period end date.</param>
    /// <returns>The settlement summary.</returns>
    Task<SettlementSummary> GetSettlementSummaryAsync(int storeId, DateTime periodStartDate, DateTime periodEndDate);

    /// <summary>
    /// Finalizes a settlement, preventing further modifications.
    /// </summary>
    /// <param name="settlementId">The settlement ID.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> FinalizeSettlementAsync(int settlementId);

    /// <summary>
    /// Adds an adjustment to a settlement.
    /// </summary>
    /// <param name="settlementId">The settlement ID.</param>
    /// <param name="type">The adjustment type.</param>
    /// <param name="amount">The adjustment amount.</param>
    /// <param name="description">The adjustment description.</param>
    /// <param name="relatedSettlementId">The related settlement ID (for prior period adjustments).</param>
    /// <param name="createdByUserId">The admin user ID creating the adjustment.</param>
    /// <returns>The created adjustment.</returns>
    Task<SettlementAdjustment> AddAdjustmentAsync(
        int settlementId,
        SettlementAdjustmentType type,
        decimal amount,
        string description,
        int? relatedSettlementId = null,
        int? createdByUserId = null);

    /// <summary>
    /// Exports a settlement to CSV format.
    /// </summary>
    /// <param name="settlementId">The settlement ID.</param>
    /// <returns>The CSV content as a byte array.</returns>
    Task<byte[]> ExportSettlementToCsvAsync(int settlementId);

    /// <summary>
    /// Gets or creates the settlement configuration.
    /// </summary>
    /// <returns>The settlement configuration.</returns>
    Task<SettlementConfig> GetOrCreateSettlementConfigAsync();

    /// <summary>
    /// Updates the settlement configuration.
    /// </summary>
    /// <param name="config">The settlement configuration.</param>
    /// <returns>The updated configuration.</returns>
    Task<SettlementConfig> UpdateSettlementConfigAsync(SettlementConfig config);
}
