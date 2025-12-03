using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Result of a resource authorization check.
/// </summary>
public class ResourceAuthorizationResult
{
    /// <summary>
    /// Gets or sets whether the authorization was successful.
    /// </summary>
    public bool IsAuthorized { get; set; }

    /// <summary>
    /// Gets or sets the reason for authorization failure.
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Creates a successful authorization result.
    /// </summary>
    public static ResourceAuthorizationResult Success() => new() { IsAuthorized = true };

    /// <summary>
    /// Creates a failed authorization result with the specified reason.
    /// </summary>
    public static ResourceAuthorizationResult Fail(string reason) => 
        new() { IsAuthorized = false, FailureReason = reason };
}

/// <summary>
/// Interface for resource-level authorization service.
/// Provides multi-tenant isolation and ownership validation for resources.
/// </summary>
public interface IResourceAuthorizationService
{
    /// <summary>
    /// Checks if a seller (via their store) owns a specific product.
    /// </summary>
    /// <param name="userId">The seller user ID.</param>
    /// <param name="productId">The product ID to check.</param>
    /// <returns>Authorization result with the store ID if successful.</returns>
    Task<(ResourceAuthorizationResult Result, int? StoreId)> AuthorizeProductAccessAsync(int userId, int productId);

    /// <summary>
    /// Checks if a seller (via their store) owns a specific sub-order.
    /// </summary>
    /// <param name="userId">The seller user ID.</param>
    /// <param name="subOrderId">The sub-order ID to check.</param>
    /// <returns>Authorization result with the store ID if successful.</returns>
    Task<(ResourceAuthorizationResult Result, int? StoreId)> AuthorizeSubOrderAccessAsync(int userId, int subOrderId);

    /// <summary>
    /// Checks if a buyer owns a specific order.
    /// </summary>
    /// <param name="userId">The buyer user ID.</param>
    /// <param name="orderId">The order ID to check.</param>
    /// <returns>Authorization result.</returns>
    Task<ResourceAuthorizationResult> AuthorizeOrderAccessAsync(int userId, int orderId);

    /// <summary>
    /// Gets the store ID for a seller user.
    /// </summary>
    /// <param name="userId">The seller user ID.</param>
    /// <returns>The store ID if found, null otherwise.</returns>
    Task<int?> GetStoreIdForSellerAsync(int userId);

    /// <summary>
    /// Validates that a store exists and belongs to the specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="storeId">The store ID to validate.</param>
    /// <returns>Authorization result.</returns>
    Task<ResourceAuthorizationResult> ValidateStoreOwnershipAsync(int userId, int storeId);
}
