namespace MercatoApp.Services;

/// <summary>
/// Feature flags for gating Phase 2 functionality.
/// </summary>
public interface IFeatureFlagService
{
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

    public FeatureFlagService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    public bool IsSellerUserManagementEnabled =>
        _configuration.GetValue<bool>("FeatureFlags:SellerUserManagement", false);

    /// <inheritdoc />
    public bool IsPromoCodeEnabled =>
        _configuration.GetValue<bool>("FeatureFlags:PromoCode", false);
}
