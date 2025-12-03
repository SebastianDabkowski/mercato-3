using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for feature flag management operations.
/// </summary>
public interface IFeatureFlagManagementService
{
    /// <summary>
    /// Gets all feature flags with optional filtering.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active flags.</param>
    /// <returns>List of feature flags.</returns>
    Task<List<FeatureFlag>> GetAllFlagsAsync(bool activeOnly = false);

    /// <summary>
    /// Gets a feature flag by its ID.
    /// </summary>
    /// <param name="id">The feature flag ID.</param>
    /// <returns>The feature flag, or null if not found.</returns>
    Task<FeatureFlag?> GetFlagByIdAsync(int id);

    /// <summary>
    /// Gets a feature flag by its key.
    /// </summary>
    /// <param name="key">The feature flag key.</param>
    /// <returns>The feature flag, or null if not found.</returns>
    Task<FeatureFlag?> GetFlagByKeyAsync(string key);

    /// <summary>
    /// Creates a new feature flag.
    /// </summary>
    /// <param name="flag">The feature flag to create.</param>
    /// <param name="createdByUserId">The ID of the user creating the flag.</param>
    /// <param name="ipAddress">The IP address of the requester.</param>
    /// <param name="userAgent">The user agent of the requester.</param>
    /// <returns>The created feature flag.</returns>
    Task<FeatureFlag> CreateFlagAsync(FeatureFlag flag, int createdByUserId, string? ipAddress, string? userAgent);

    /// <summary>
    /// Updates an existing feature flag.
    /// </summary>
    /// <param name="flag">The feature flag to update.</param>
    /// <param name="updatedByUserId">The ID of the user updating the flag.</param>
    /// <param name="ipAddress">The IP address of the requester.</param>
    /// <param name="userAgent">The user agent of the requester.</param>
    /// <returns>The updated feature flag.</returns>
    Task<FeatureFlag> UpdateFlagAsync(FeatureFlag flag, int updatedByUserId, string? ipAddress, string? userAgent);

    /// <summary>
    /// Deletes a feature flag.
    /// </summary>
    /// <param name="id">The ID of the feature flag to delete.</param>
    /// <param name="deletedByUserId">The ID of the user deleting the flag.</param>
    /// <param name="ipAddress">The IP address of the requester.</param>
    /// <param name="userAgent">The user agent of the requester.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteFlagAsync(int id, int deletedByUserId, string? ipAddress, string? userAgent);

    /// <summary>
    /// Toggles a feature flag's enabled state.
    /// </summary>
    /// <param name="id">The ID of the feature flag to toggle.</param>
    /// <param name="isEnabled">The new enabled state.</param>
    /// <param name="updatedByUserId">The ID of the user toggling the flag.</param>
    /// <param name="ipAddress">The IP address of the requester.</param>
    /// <param name="userAgent">The user agent of the requester.</param>
    /// <returns>The updated feature flag.</returns>
    Task<FeatureFlag?> ToggleFlagAsync(int id, bool isEnabled, int updatedByUserId, string? ipAddress, string? userAgent);

    /// <summary>
    /// Gets the history of changes for a feature flag.
    /// </summary>
    /// <param name="featureFlagId">The ID of the feature flag.</param>
    /// <returns>List of history entries.</returns>
    Task<List<FeatureFlagHistory>> GetFlagHistoryAsync(int featureFlagId);
}
