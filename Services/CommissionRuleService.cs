using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing commission rules with effective dates and conflict detection.
/// </summary>
public class CommissionRuleService : ICommissionRuleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CommissionRuleService> _logger;

    public CommissionRuleService(
        ApplicationDbContext context,
        ILogger<CommissionRuleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<CommissionRule>> GetAllRulesAsync(bool activeOnly = false)
    {
        var query = _context.CommissionRules
            .Include(r => r.CreatedByUser)
            .Include(r => r.UpdatedByUser)
            .Include(r => r.Category)
            .Include(r => r.Store)
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(r => r.IsActive);
        }

        return await query
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CommissionRule?> GetRuleByIdAsync(int id)
    {
        return await _context.CommissionRules
            .Include(r => r.CreatedByUser)
            .Include(r => r.UpdatedByUser)
            .Include(r => r.Category)
            .Include(r => r.Store)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <inheritdoc />
    public async Task<CommissionRule> CreateRuleAsync(CommissionRule rule, int currentUserId)
    {
        // Validate for conflicts
        var conflicts = await ValidateRuleConflictsAsync(rule);
        if (conflicts.Any())
        {
            var conflictDetails = string.Join(", ", conflicts.Select(c => $"Rule #{c.Id} ({c.Name})"));
            throw new InvalidOperationException(
                $"The commission rule conflicts with existing rules: {conflictDetails}. " +
                "Please adjust the effective dates, applicability, or priority to resolve the conflict.");
        }

        // Set audit fields
        rule.CreatedByUserId = currentUserId;
        rule.CreatedAt = DateTime.UtcNow;

        // Validate applicability-specific fields
        ValidateApplicabilityFields(rule);

        _context.CommissionRules.Add(rule);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Commission rule created: {RuleName} (ID: {RuleId}) by user {UserId}",
            rule.Name, rule.Id, currentUserId);

        return rule;
    }

    /// <inheritdoc />
    public async Task<CommissionRule> UpdateRuleAsync(CommissionRule rule, int currentUserId)
    {
        var existingRule = await _context.CommissionRules.FindAsync(rule.Id);
        if (existingRule == null)
        {
            throw new InvalidOperationException($"Commission rule with ID {rule.Id} not found.");
        }

        // Validate for conflicts (excluding this rule)
        var conflicts = await ValidateRuleConflictsAsync(rule, rule.Id);
        if (conflicts.Any())
        {
            var conflictDetails = string.Join(", ", conflicts.Select(c => $"Rule #{c.Id} ({c.Name})"));
            throw new InvalidOperationException(
                $"The commission rule conflicts with existing rules: {conflictDetails}. " +
                "Please adjust the effective dates, applicability, or priority to resolve the conflict.");
        }

        // Validate applicability-specific fields
        ValidateApplicabilityFields(rule);

        // Update fields
        existingRule.Name = rule.Name;
        existingRule.CommissionPercentage = rule.CommissionPercentage;
        existingRule.FixedCommissionAmount = rule.FixedCommissionAmount;
        existingRule.ApplicabilityType = rule.ApplicabilityType;
        existingRule.CategoryId = rule.CategoryId;
        existingRule.StoreId = rule.StoreId;
        existingRule.SellerTier = rule.SellerTier;
        existingRule.EffectiveStartDate = rule.EffectiveStartDate;
        existingRule.EffectiveEndDate = rule.EffectiveEndDate;
        existingRule.Priority = rule.Priority;
        existingRule.IsActive = rule.IsActive;
        existingRule.Notes = rule.Notes;
        existingRule.UpdatedByUserId = currentUserId;
        existingRule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Commission rule updated: {RuleName} (ID: {RuleId}) by user {UserId}",
            rule.Name, rule.Id, currentUserId);

        return existingRule;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteRuleAsync(int id)
    {
        var rule = await _context.CommissionRules.FindAsync(id);
        if (rule == null)
        {
            return false;
        }

        _context.CommissionRules.Remove(rule);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Commission rule deleted: ID {RuleId}", id);
        return true;
    }

    /// <inheritdoc />
    public async Task<List<CommissionRule>> ValidateRuleConflictsAsync(CommissionRule rule, int? excludeRuleId = null)
    {
        // Get all active rules that could potentially conflict
        var query = _context.CommissionRules
            .Where(r => r.IsActive && r.ApplicabilityType == rule.ApplicabilityType)
            .AsQueryable();

        // Exclude the rule being updated
        if (excludeRuleId.HasValue)
        {
            query = query.Where(r => r.Id != excludeRuleId.Value);
        }

        // Filter by applicability-specific criteria
        switch (rule.ApplicabilityType)
        {
            case CommissionRuleApplicability.Category:
                query = query.Where(r => r.CategoryId == rule.CategoryId);
                break;
            case CommissionRuleApplicability.Seller:
                query = query.Where(r => r.StoreId == rule.StoreId);
                break;
            case CommissionRuleApplicability.SellerTier:
                query = query.Where(r => r.SellerTier == rule.SellerTier);
                break;
            case CommissionRuleApplicability.Global:
                // Global rules can conflict with any other global rule
                break;
        }

        var potentialConflicts = await query.ToListAsync();

        // Check for date range overlaps
        var conflicts = potentialConflicts.Where(existing =>
        {
            // Check if date ranges overlap
            var existingStart = existing.EffectiveStartDate;
            var existingEnd = existing.EffectiveEndDate ?? DateTime.MaxValue;
            var newStart = rule.EffectiveStartDate;
            var newEnd = rule.EffectiveEndDate ?? DateTime.MaxValue;

            // Date ranges overlap if:
            // new start is before existing end AND new end is after existing start
            return newStart < existingEnd && newEnd > existingStart;
        }).ToList();

        return conflicts;
    }

    /// <inheritdoc />
    public async Task<CommissionRule?> GetApplicableRuleAsync(
        DateTime transactionDate,
        int storeId,
        int? categoryId = null,
        string? sellerTier = null)
    {
        // Build query for active rules effective at transaction date
        var query = _context.CommissionRules
            .Where(r => r.IsActive)
            .Where(r => r.EffectiveStartDate <= transactionDate)
            .Where(r => r.EffectiveEndDate == null || r.EffectiveEndDate >= transactionDate)
            .AsQueryable();

        // Priority order: Category > Seller > SellerTier > Global
        // Try category-specific rule first
        if (categoryId.HasValue)
        {
            var categoryRule = await query
                .Where(r => r.ApplicabilityType == CommissionRuleApplicability.Category)
                .Where(r => r.CategoryId == categoryId.Value)
                .OrderByDescending(r => r.Priority)
                .ThenByDescending(r => r.EffectiveStartDate)
                .FirstOrDefaultAsync();

            if (categoryRule != null)
            {
                _logger.LogDebug(
                    "Found category-specific commission rule {RuleId} for category {CategoryId}",
                    categoryRule.Id, categoryId.Value);
                return categoryRule;
            }
        }

        // Try seller-specific rule
        var sellerRule = await query
            .Where(r => r.ApplicabilityType == CommissionRuleApplicability.Seller)
            .Where(r => r.StoreId == storeId)
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.EffectiveStartDate)
            .FirstOrDefaultAsync();

        if (sellerRule != null)
        {
            _logger.LogDebug(
                "Found seller-specific commission rule {RuleId} for store {StoreId}",
                sellerRule.Id, storeId);
            return sellerRule;
        }

        // Try seller tier rule
        if (!string.IsNullOrEmpty(sellerTier))
        {
            var tierRule = await query
                .Where(r => r.ApplicabilityType == CommissionRuleApplicability.SellerTier)
                .Where(r => r.SellerTier == sellerTier)
                .OrderByDescending(r => r.Priority)
                .ThenByDescending(r => r.EffectiveStartDate)
                .FirstOrDefaultAsync();

            if (tierRule != null)
            {
                _logger.LogDebug(
                    "Found tier-specific commission rule {RuleId} for tier {Tier}",
                    tierRule.Id, sellerTier);
                return tierRule;
            }
        }

        // Fall back to global rule
        var globalRule = await query
            .Where(r => r.ApplicabilityType == CommissionRuleApplicability.Global)
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.EffectiveStartDate)
            .FirstOrDefaultAsync();

        if (globalRule != null)
        {
            _logger.LogDebug("Found global commission rule {RuleId}", globalRule.Id);
            return globalRule;
        }

        _logger.LogWarning(
            "No applicable commission rule found for transaction date {Date}, store {StoreId}",
            transactionDate, storeId);
        return null;
    }

    /// <inheritdoc />
    public async Task<List<CommissionRule>> GetFutureRulesAsync()
    {
        var now = DateTime.UtcNow.Date;
        return await _context.CommissionRules
            .Include(r => r.CreatedByUser)
            .Include(r => r.Category)
            .Include(r => r.Store)
            .Where(r => r.IsActive && r.EffectiveStartDate > now)
            .OrderBy(r => r.EffectiveStartDate)
            .ThenByDescending(r => r.Priority)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<CommissionRule>> GetAuditHistoryAsync(
        int? ruleId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _context.CommissionRules
            .Include(r => r.CreatedByUser)
            .Include(r => r.UpdatedByUser)
            .Include(r => r.Category)
            .Include(r => r.Store)
            .AsQueryable();

        if (ruleId.HasValue)
        {
            query = query.Where(r => r.Id == ruleId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= fromDate.Value || 
                                    (r.UpdatedAt.HasValue && r.UpdatedAt >= fromDate.Value));
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= toDate.Value);
        }

        return await query
            .OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Validates that applicability-specific fields are set correctly.
    /// </summary>
    private void ValidateApplicabilityFields(CommissionRule rule)
    {
        switch (rule.ApplicabilityType)
        {
            case CommissionRuleApplicability.Category:
                if (!rule.CategoryId.HasValue)
                {
                    throw new InvalidOperationException(
                        "CategoryId is required for category-specific rules.");
                }
                rule.StoreId = null;
                rule.SellerTier = null;
                break;

            case CommissionRuleApplicability.Seller:
                if (!rule.StoreId.HasValue)
                {
                    throw new InvalidOperationException(
                        "StoreId is required for seller-specific rules.");
                }
                rule.CategoryId = null;
                rule.SellerTier = null;
                break;

            case CommissionRuleApplicability.SellerTier:
                if (string.IsNullOrWhiteSpace(rule.SellerTier))
                {
                    throw new InvalidOperationException(
                        "SellerTier is required for tier-specific rules.");
                }
                rule.CategoryId = null;
                rule.StoreId = null;
                break;

            case CommissionRuleApplicability.Global:
                rule.CategoryId = null;
                rule.StoreId = null;
                rule.SellerTier = null;
                break;

            default:
                throw new InvalidOperationException(
                    $"Invalid applicability type: {rule.ApplicabilityType}");
        }
    }
}
