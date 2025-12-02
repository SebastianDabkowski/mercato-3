using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for shipping method management service.
/// </summary>
public interface IShippingMethodService
{
    /// <summary>
    /// Gets all active shipping methods for a store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>A list of active shipping methods.</returns>
    Task<List<ShippingMethod>> GetActiveShippingMethodsAsync(int storeId);

    /// <summary>
    /// Gets or creates default shipping methods for a store.
    /// If the store has no shipping methods, creates default ones.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>A list of shipping methods.</returns>
    Task<List<ShippingMethod>> GetOrCreateDefaultShippingMethodsAsync(int storeId);

    /// <summary>
    /// Gets a shipping method by ID.
    /// </summary>
    /// <param name="id">The shipping method ID.</param>
    /// <returns>The shipping method, or null if not found.</returns>
    Task<ShippingMethod?> GetShippingMethodByIdAsync(int id);

    /// <summary>
    /// Calculates the shipping cost for a specific method and cart items.
    /// </summary>
    /// <param name="shippingMethodId">The shipping method ID.</param>
    /// <param name="items">The cart items.</param>
    /// <returns>The calculated shipping cost.</returns>
    Task<decimal> CalculateShippingCostAsync(int shippingMethodId, List<CartItem> items);

    /// <summary>
    /// Gets all shipping methods for a store (including inactive ones).
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>A list of all shipping methods.</returns>
    Task<List<ShippingMethod>> GetAllShippingMethodsAsync(int storeId);

    /// <summary>
    /// Creates a new shipping method for a store.
    /// </summary>
    /// <param name="shippingMethod">The shipping method to create.</param>
    /// <returns>The created shipping method.</returns>
    Task<ShippingMethod> CreateShippingMethodAsync(ShippingMethod shippingMethod);

    /// <summary>
    /// Updates an existing shipping method.
    /// </summary>
    /// <param name="shippingMethod">The shipping method with updated values.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> UpdateShippingMethodAsync(ShippingMethod shippingMethod);

    /// <summary>
    /// Deletes (soft delete by setting IsActive to false) a shipping method.
    /// </summary>
    /// <param name="id">The shipping method ID.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> DeleteShippingMethodAsync(int id);
}
