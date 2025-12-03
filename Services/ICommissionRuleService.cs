using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for commission rule management service.
/// Handles CRUD operations and conflict detection for commission rules.
/// </summary>
public interface ICommissionRuleService
{
    /// <summary>
    /// Gets all commission rules, optionally filtered by active status.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active rules.</param>
    /// <returns>A list of commission rules.</returns>
    Task<List<CommissionRule>> GetAllRulesAsync(bool activeOnly = false);

    /// <summary>
    /// Gets a commission rule by ID.
    /// </summary>
    /// <param name="id">The rule ID.</param>
    /// <returns>The commission rule or null if not found.</returns>
    Task<CommissionRule?> GetRuleByIdAsync(int id);

    /// <summary>
    /// Creates a new commission rule with conflict validation.
    /// </summary>
    /// <param name="rule">The rule to create.</param>
    /// <param name="currentUserId">The ID of the user creating the rule.</param>
    /// <returns>The created commission rule.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the rule conflicts with existing rules.</exception>
    Task<CommissionRule> CreateRuleAsync(CommissionRule rule, int currentUserId);

    /// <summary>
    /// Updates an existing commission rule with conflict validation.
    /// </summary>
    /// <param name="rule">The rule to update.</param>
    /// <param name="currentUserId">The ID of the user updating the rule.</param>
    /// <returns>The updated commission rule.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the rule conflicts with existing rules.</exception>
    Task<CommissionRule> UpdateRuleAsync(CommissionRule rule, int currentUserId);

    /// <summary>
    /// Deletes a commission rule.
    /// </summary>
    /// <param name="id">The rule ID to delete.</param>
    /// <returns>True if deleted successfully, false otherwise.</returns>
    Task<bool> DeleteRuleAsync(int id);

    /// <summary>
    /// Validates a commission rule for conflicts with existing rules.
    /// </summary>
    /// <param name="rule">The rule to validate.</param>
    /// <param name="excludeRuleId">Optional rule ID to exclude from conflict checking (for updates).</param>
    /// <returns>A list of conflicting rules, or empty if no conflicts.</returns>
    Task<List<CommissionRule>> ValidateRuleConflictsAsync(CommissionRule rule, int? excludeRuleId = null);

    /// <summary>
    /// Gets the applicable commission rule for a specific transaction.
    /// Considers effective dates, applicability, and priority.
    /// </summary>
    /// <param name="transactionDate">The transaction date.</param>
    /// <param name="storeId">The store ID.</param>
    /// <param name="categoryId">Optional category ID.</param>
    /// <param name="sellerTier">Optional seller tier.</param>
    /// <returns>The applicable commission rule, or null if none applies.</returns>
    Task<CommissionRule?> GetApplicableRuleAsync(
        DateTime transactionDate,
        int storeId,
        int? categoryId = null,
        string? sellerTier = null);

    /// <summary>
    /// Gets commission rules that will become effective in the future.
    /// </summary>
    /// <returns>A list of future-dated commission rules.</returns>
    Task<List<CommissionRule>> GetFutureRulesAsync();

    /// <summary>
    /// Gets audit history for commission rules.
    /// </summary>
    /// <param name="ruleId">Optional rule ID to filter by.</param>
    /// <param name="fromDate">Optional start date.</param>
    /// <param name="toDate">Optional end date.</param>
    /// <returns>A list of commission rules for audit purposes.</returns>
    Task<List<CommissionRule>> GetAuditHistoryAsync(
        int? ruleId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);
}
