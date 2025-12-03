using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Feature flags for gating Phase 2 functionality.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Checks if a feature is enabled for the current context.
    /// </summary>
    /// <param name="featureKey">The feature flag key.</param>
    /// <param name="userId">The user ID (optional).</param>
    /// <param name="userRole">The user role (optional).</param>
    /// <param name="storeId">The store ID (optional).</param>
    /// <param name="environment">The current environment (optional, defaults to configuration).</param>
    /// <returns>True if the feature is enabled, false otherwise.</returns>
    Task<bool> IsEnabledAsync(string featureKey, int? userId = null, string? userRole = null, int? storeId = null, string? environment = null);

    /// <summary>
    /// Checks if the seller internal user management feature is enabled.
    /// This is a Phase 2 feature and is disabled by default.
    /// </summary>
    bool IsSellerUserManagementEnabled { get; }

    /// <summary>
    /// Checks if the promo code feature is enabled.
    /// This is a Phase 2 feature and is disabled by default.
    /// </summary>
    bool IsPromoCodeEnabled { get; }
}

/// <summary>
/// Implementation of feature flag service.
/// Configuration-based feature flags for Phase 2 functionality.
/// </summary>
public class FeatureFlagService : IFeatureFlagService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public FeatureFlagService(IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public bool IsSellerUserManagementEnabled =>
        _configuration.GetValue<bool>("FeatureFlags:SellerUserManagement", false);

    /// <inheritdoc />
    public bool IsPromoCodeEnabled =>
        _configuration.GetValue<bool>("FeatureFlags:PromoCode", false);

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(string featureKey, int? userId = null, string? userRole = null, int? storeId = null, string? environment = null)
    {
        // Create a scope to access scoped services
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

        // Get the feature flag from the database
        var flag = await context.FeatureFlags
            .Include(f => f.Rules.OrderBy(r => r.Priority))
            .FirstOrDefaultAsync(f => f.Key == featureKey && f.IsActive);

        if (flag == null)
        {
            // Fall back to configuration-based flags for backward compatibility
            return _configuration.GetValue<bool>($"FeatureFlags:{featureKey}", false);
        }

        // Determine the current environment
        var currentEnvironment = environment ?? _configuration.GetValue<string>("Environment", "dev");

        // Check if the flag is applicable to the current environment
        if (!string.IsNullOrEmpty(flag.Environments))
        {
            var applicableEnvironments = flag.Environments.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().ToLowerInvariant());

            if (!applicableEnvironments.Contains(currentEnvironment.ToLowerInvariant()))
            {
                return false; // Flag not applicable to this environment
            }
        }

        // Evaluate rules in priority order
        foreach (var rule in flag.Rules)
        {
            if (EvaluateRule(rule, userId, userRole, storeId, currentEnvironment))
            {
                return rule.IsEnabled;
            }
        }

        // No rules matched, return the default state
        return flag.IsEnabledByDefault;
    }

    private bool EvaluateRule(Models.FeatureFlagRule rule, int? userId, string? userRole, int? storeId, string currentEnvironment)
    {
        switch (rule.RuleType)
        {
            case Models.FeatureFlagRuleType.UserRole:
                if (userRole != null && !string.IsNullOrEmpty(rule.RuleValue))
                {
                    var targetRoles = rule.RuleValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(r => r.Trim());
                    return targetRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
                }
                return false;

            case Models.FeatureFlagRuleType.UserId:
                if (userId.HasValue && !string.IsNullOrEmpty(rule.RuleValue))
                {
                    var targetUserIds = rule.RuleValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => int.TryParse(id.Trim(), out var parsedId) ? parsedId : (int?)null)
                        .Where(id => id.HasValue)
                        .Select(id => id!.Value);
                    return targetUserIds.Contains(userId.Value);
                }
                return false;

            case Models.FeatureFlagRuleType.StoreId:
                if (storeId.HasValue && !string.IsNullOrEmpty(rule.RuleValue))
                {
                    var targetStoreIds = rule.RuleValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => int.TryParse(id.Trim(), out var parsedId) ? parsedId : (int?)null)
                        .Where(id => id.HasValue)
                        .Select(id => id!.Value);
                    return targetStoreIds.Contains(storeId.Value);
                }
                return false;

            case Models.FeatureFlagRuleType.PercentageRollout:
                if (userId.HasValue && !string.IsNullOrEmpty(rule.RuleValue) &&
                    int.TryParse(rule.RuleValue, out var percentage))
                {
                    // Use consistent hashing to ensure the same user always gets the same result
                    var hash = HashUserId(userId.Value, rule.Id);
                    var userPercentile = hash % 100;
                    return userPercentile < percentage;
                }
                return false;

            case Models.FeatureFlagRuleType.Environment:
                if (!string.IsNullOrEmpty(rule.RuleValue))
                {
                    var targetEnvironments = rule.RuleValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(e => e.Trim());
                    return targetEnvironments.Contains(currentEnvironment, StringComparer.OrdinalIgnoreCase);
                }
                return false;

            default:
                return false;
        }
    }

    private int HashUserId(int userId, int ruleId)
    {
        // Simple hash function for consistent percentage rollout
        // Combines userId and ruleId to ensure different rules give different results
        var combined = $"{userId}-{ruleId}";
        var hash = 0;
        foreach (var c in combined)
        {
            hash = (hash * 31 + c) & 0x7FFFFFFF; // Keep positive
        }
        return hash;
    }
}
