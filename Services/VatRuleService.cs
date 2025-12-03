using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing VAT rules.
/// Implements CRUD operations and conflict detection for VAT rules.
/// </summary>
public class VatRuleService : IVatRuleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VatRuleService> _logger;

    public VatRuleService(
        ApplicationDbContext context,
        ILogger<VatRuleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<VatRule>> GetAllRulesAsync(bool activeOnly = false)
    {
        var query = _context.VatRules
            .Include(r => r.Category)
            .Include(r => r.CreatedByUser)
            .Include(r => r.UpdatedByUser)
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(r => r.IsActive);
        }

        return await query
            .OrderBy(r => r.CountryCode)
            .ThenBy(r => r.RegionCode)
            .ThenBy(r => r.EffectiveStartDate)
            .ThenByDescending(r => r.Priority)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<VatRule?> GetRuleByIdAsync(int id)
    {
        return await _context.VatRules
            .Include(r => r.Category)
            .Include(r => r.CreatedByUser)
            .Include(r => r.UpdatedByUser)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <inheritdoc />
    public async Task<VatRule> CreateRuleAsync(VatRule rule, int currentUserId)
    {
        // Validate for conflicts
        var conflicts = await ValidateRuleConflictsAsync(rule);
        if (conflicts.Any())
        {
            var conflictNames = string.Join(", ", conflicts.Select(c => c.Name));
            throw new InvalidOperationException(
                $"VAT rule conflicts with existing rules: {conflictNames}. " +
                "Please review effective dates and priorities.");
        }

        // Set audit fields
        rule.CreatedByUserId = currentUserId;
        rule.CreatedAt = DateTime.UtcNow;

        _context.VatRules.Add(rule);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "VAT rule {RuleName} created by user {UserId}",
            rule.Name, currentUserId);

        return rule;
    }

    /// <inheritdoc />
    public async Task<VatRule> UpdateRuleAsync(VatRule rule, int currentUserId)
    {
        var existing = await _context.VatRules.FindAsync(rule.Id);
        if (existing == null)
        {
            throw new InvalidOperationException($"VAT rule with ID {rule.Id} not found.");
        }

        // Validate for conflicts (excluding this rule)
        var conflicts = await ValidateRuleConflictsAsync(rule, rule.Id);
        if (conflicts.Any())
        {
            var conflictNames = string.Join(", ", conflicts.Select(c => c.Name));
            throw new InvalidOperationException(
                $"VAT rule conflicts with existing rules: {conflictNames}. " +
                "Please review effective dates and priorities.");
        }

        // Update properties
        existing.Name = rule.Name;
        existing.TaxPercentage = rule.TaxPercentage;
        existing.CountryCode = rule.CountryCode;
        existing.RegionCode = rule.RegionCode;
        existing.ApplicabilityType = rule.ApplicabilityType;
        existing.CategoryId = rule.CategoryId;
        existing.EffectiveStartDate = rule.EffectiveStartDate;
        existing.EffectiveEndDate = rule.EffectiveEndDate;
        existing.Priority = rule.Priority;
        existing.IsActive = rule.IsActive;
        existing.Notes = rule.Notes;

        // Set audit fields
        existing.UpdatedByUserId = currentUserId;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "VAT rule {RuleName} updated by user {UserId}",
            existing.Name, currentUserId);

        return existing;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteRuleAsync(int id)
    {
        var rule = await _context.VatRules.FindAsync(id);
        if (rule == null)
        {
            return false;
        }

        _context.VatRules.Remove(rule);
        await _context.SaveChangesAsync();

        _logger.LogInformation("VAT rule {RuleName} deleted", rule.Name);

        return true;
    }

    /// <inheritdoc />
    public async Task<List<VatRule>> ValidateRuleConflictsAsync(VatRule rule, int? excludeRuleId = null)
    {
        // Find rules with overlapping date ranges for the same country/region/category
        var query = _context.VatRules
            .Where(r => r.IsActive)
            .Where(r => r.CountryCode == rule.CountryCode)
            .AsQueryable();

        // Exclude the current rule if updating
        if (excludeRuleId.HasValue)
        {
            query = query.Where(r => r.Id != excludeRuleId.Value);
        }

        // Match region (both null or same value)
        if (rule.RegionCode == null)
        {
            query = query.Where(r => r.RegionCode == null);
        }
        else
        {
            query = query.Where(r => r.RegionCode == rule.RegionCode);
        }

        // Match applicability
        if (rule.ApplicabilityType == VatRuleApplicability.Global)
        {
            query = query.Where(r => r.ApplicabilityType == VatRuleApplicability.Global);
        }
        else if (rule.ApplicabilityType == VatRuleApplicability.Category && rule.CategoryId.HasValue)
        {
            query = query.Where(r =>
                r.ApplicabilityType == VatRuleApplicability.Category &&
                r.CategoryId == rule.CategoryId.Value);
        }

        var potentialConflicts = await query.ToListAsync();

        // Check for date overlaps with same priority
        var conflicts = potentialConflicts.Where(existing =>
        {
            // Different priorities don't conflict (explicit override)
            if (existing.Priority != rule.Priority)
            {
                return false;
            }

            var existingStart = existing.EffectiveStartDate;
            var existingEnd = existing.EffectiveEndDate ?? DateTime.MaxValue;
            var newStart = rule.EffectiveStartDate;
            var newEnd = rule.EffectiveEndDate ?? DateTime.MaxValue;

            // Check if date ranges overlap
            return newStart <= existingEnd && newEnd >= existingStart;
        }).ToList();

        return conflicts;
    }

    /// <inheritdoc />
    public async Task<VatRule?> GetApplicableRuleAsync(
        DateTime transactionDate,
        string countryCode,
        string? regionCode = null,
        int? categoryId = null)
    {
        var query = _context.VatRules
            .Where(r => r.IsActive)
            .Where(r => r.CountryCode == countryCode)
            .Where(r => r.EffectiveStartDate <= transactionDate)
            .Where(r => r.EffectiveEndDate == null || r.EffectiveEndDate >= transactionDate)
            .AsQueryable();

        // Match region
        if (regionCode != null)
        {
            query = query.Where(r => r.RegionCode == null || r.RegionCode == regionCode);
        }
        else
        {
            query = query.Where(r => r.RegionCode == null);
        }

        // Category-specific rules have higher priority than global rules
        var applicableRules = await query
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.ApplicabilityType == VatRuleApplicability.Category ? 1 : 0)
            .ToListAsync();

        // If category is specified, prefer category-specific rule
        if (categoryId.HasValue)
        {
            var categoryRule = applicableRules.FirstOrDefault(r =>
                r.ApplicabilityType == VatRuleApplicability.Category &&
                r.CategoryId == categoryId.Value);

            if (categoryRule != null)
            {
                return categoryRule;
            }
        }

        // Fall back to global rule
        return applicableRules.FirstOrDefault(r =>
            r.ApplicabilityType == VatRuleApplicability.Global);
    }

    /// <inheritdoc />
    public async Task<List<VatRule>> GetFutureRulesAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.VatRules
            .Include(r => r.Category)
            .Include(r => r.CreatedByUser)
            .Where(r => r.IsActive && r.EffectiveStartDate > now)
            .OrderBy(r => r.EffectiveStartDate)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<VatRule>> GetAuditHistoryAsync(
        int? ruleId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _context.VatRules
            .Include(r => r.Category)
            .Include(r => r.CreatedByUser)
            .Include(r => r.UpdatedByUser)
            .AsQueryable();

        if (ruleId.HasValue)
        {
            query = query.Where(r => r.Id == ruleId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= fromDate.Value || (r.UpdatedAt.HasValue && r.UpdatedAt >= fromDate.Value));
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => (r.UpdatedAt ?? r.CreatedAt) <= toDate.Value);
        }

        return await query
            .OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<VatRule>> GetRulesByCountryAsync(string countryCode, string? regionCode = null)
    {
        var query = _context.VatRules
            .Include(r => r.Category)
            .Where(r => r.CountryCode == countryCode)
            .AsQueryable();

        if (regionCode != null)
        {
            query = query.Where(r => r.RegionCode == null || r.RegionCode == regionCode);
        }

        return await query
            .OrderBy(r => r.RegionCode)
            .ThenBy(r => r.EffectiveStartDate)
            .ThenByDescending(r => r.Priority)
            .ToListAsync();
    }
}
