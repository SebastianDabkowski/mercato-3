using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for VAT rule management service.
/// Handles CRUD operations and conflict detection for VAT rules.
/// </summary>
public interface IVatRuleService
{
    /// <summary>
    /// Gets all VAT rules, optionally filtered by active status.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active rules.</param>
    /// <returns>A list of VAT rules.</returns>
    Task<List<VatRule>> GetAllRulesAsync(bool activeOnly = false);

    /// <summary>
    /// Gets a VAT rule by ID.
    /// </summary>
    /// <param name="id">The rule ID.</param>
    /// <returns>The VAT rule or null if not found.</returns>
    Task<VatRule?> GetRuleByIdAsync(int id);

    /// <summary>
    /// Creates a new VAT rule with conflict validation.
    /// </summary>
    /// <param name="rule">The rule to create.</param>
    /// <param name="currentUserId">The ID of the user creating the rule.</param>
    /// <returns>The created VAT rule.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the rule conflicts with existing rules.</exception>
    Task<VatRule> CreateRuleAsync(VatRule rule, int currentUserId);

    /// <summary>
    /// Updates an existing VAT rule with conflict validation.
    /// </summary>
    /// <param name="rule">The rule to update.</param>
    /// <param name="currentUserId">The ID of the user updating the rule.</param>
    /// <returns>The updated VAT rule.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the rule conflicts with existing rules.</exception>
    Task<VatRule> UpdateRuleAsync(VatRule rule, int currentUserId);

    /// <summary>
    /// Deletes a VAT rule.
    /// </summary>
    /// <param name="id">The rule ID to delete.</param>
    /// <returns>True if deleted successfully, false otherwise.</returns>
    Task<bool> DeleteRuleAsync(int id);

    /// <summary>
    /// Validates a VAT rule for conflicts with existing rules.
    /// </summary>
    /// <param name="rule">The rule to validate.</param>
    /// <param name="excludeRuleId">Optional rule ID to exclude from conflict checking (for updates).</param>
    /// <returns>A list of conflicting rules, or empty if no conflicts.</returns>
    Task<List<VatRule>> ValidateRuleConflictsAsync(VatRule rule, int? excludeRuleId = null);

    /// <summary>
    /// Gets the applicable VAT rule for a specific transaction.
    /// Considers effective dates, country/region, category applicability, and priority.
    /// </summary>
    /// <param name="transactionDate">The transaction date.</param>
    /// <param name="countryCode">The delivery country code (ISO 3166-1 alpha-2).</param>
    /// <param name="regionCode">Optional region/state code.</param>
    /// <param name="categoryId">Optional category ID.</param>
    /// <returns>The applicable VAT rule, or null if none applies.</returns>
    Task<VatRule?> GetApplicableRuleAsync(
        DateTime transactionDate,
        string countryCode,
        string? regionCode = null,
        int? categoryId = null);

    /// <summary>
    /// Gets VAT rules that will become effective in the future.
    /// </summary>
    /// <returns>A list of future-dated VAT rules.</returns>
    Task<List<VatRule>> GetFutureRulesAsync();

    /// <summary>
    /// Gets audit history for VAT rules.
    /// </summary>
    /// <param name="ruleId">Optional rule ID to filter by.</param>
    /// <param name="fromDate">Optional start date.</param>
    /// <param name="toDate">Optional end date.</param>
    /// <returns>A list of VAT rules for audit purposes.</returns>
    Task<List<VatRule>> GetAuditHistoryAsync(
        int? ruleId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Gets all VAT rules for a specific country, optionally filtered by region.
    /// </summary>
    /// <param name="countryCode">The country code.</param>
    /// <param name="regionCode">Optional region code.</param>
    /// <returns>A list of VAT rules.</returns>
    Task<List<VatRule>> GetRulesByCountryAsync(string countryCode, string? regionCode = null);
}
