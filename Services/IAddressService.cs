using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for address management service.
/// </summary>
public interface IAddressService
{
    /// <summary>
    /// Gets all addresses for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A list of addresses.</returns>
    Task<List<Address>> GetUserAddressesAsync(int userId);

    /// <summary>
    /// Gets an address by its ID.
    /// </summary>
    /// <param name="addressId">The address ID.</param>
    /// <returns>The address, or null if not found.</returns>
    Task<Address?> GetAddressByIdAsync(int addressId);

    /// <summary>
    /// Creates a new address.
    /// </summary>
    /// <param name="address">The address to create.</param>
    /// <returns>The created address.</returns>
    Task<Address> CreateAddressAsync(Address address);

    /// <summary>
    /// Updates an existing address.
    /// </summary>
    /// <param name="address">The address to update.</param>
    /// <returns>The updated address.</returns>
    Task<Address> UpdateAddressAsync(Address address);

    /// <summary>
    /// Deletes an address.
    /// </summary>
    /// <param name="addressId">The address ID to delete.</param>
    Task DeleteAddressAsync(int addressId);

    /// <summary>
    /// Sets an address as the default for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="addressId">The address ID to set as default.</param>
    Task SetDefaultAddressAsync(int userId, int addressId);

    /// <summary>
    /// Validates if an address can ship to the specified country.
    /// </summary>
    /// <param name="countryCode">The country code to validate.</param>
    /// <returns>True if shipping is allowed to this country.</returns>
    Task<bool> IsShippingAllowedToCountryAsync(string countryCode);
}
