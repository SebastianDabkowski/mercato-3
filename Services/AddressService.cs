using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing delivery and billing addresses.
/// </summary>
public class AddressService : IAddressService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AddressService> _logger;

    // List of countries where Mercato operates and can ship to
    private static readonly HashSet<string> AllowedCountries = new()
    {
        "US", // United States
        "CA", // Canada
        "GB", // United Kingdom
        "DE", // Germany
        "FR", // France
        "IT", // Italy
        "ES", // Spain
        "AU", // Australia
        "NZ", // New Zealand
        "JP", // Japan
    };

    public AddressService(ApplicationDbContext context, ILogger<AddressService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<Address>> GetUserAddressesAsync(int userId)
    {
        return await _context.Addresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.UpdatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Address?> GetAddressByIdAsync(int addressId)
    {
        return await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId);
    }

    /// <inheritdoc />
    public async Task<Address> CreateAddressAsync(Address address)
    {
        // Validate country code
        if (!await IsShippingAllowedToCountryAsync(address.CountryCode))
        {
            throw new InvalidOperationException($"Shipping to country '{address.CountryCode}' is not currently supported.");
        }

        // If this is the first address or marked as default, set it as default
        if (address.UserId.HasValue)
        {
            var existingAddresses = await _context.Addresses
                .Where(a => a.UserId == address.UserId)
                .ToListAsync();

            if (!existingAddresses.Any())
            {
                address.IsDefault = true;
            }
            else if (address.IsDefault)
            {
                // Clear other default addresses
                foreach (var existing in existingAddresses.Where(a => a.IsDefault))
                {
                    existing.IsDefault = false;
                }
            }
        }

        address.CreatedAt = DateTime.UtcNow;
        address.UpdatedAt = DateTime.UtcNow;

        _context.Addresses.Add(address);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created address {AddressId} for user {UserId}", 
            address.Id, address.UserId ?? 0);

        return address;
    }

    /// <inheritdoc />
    public async Task<Address> UpdateAddressAsync(Address address)
    {
        // Validate country code
        if (!await IsShippingAllowedToCountryAsync(address.CountryCode))
        {
            throw new InvalidOperationException($"Shipping to country '{address.CountryCode}' is not currently supported.");
        }

        var existingAddress = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == address.Id);

        if (existingAddress == null)
        {
            throw new InvalidOperationException("Address not found.");
        }

        // Update fields
        existingAddress.FullName = address.FullName;
        existingAddress.PhoneNumber = address.PhoneNumber;
        existingAddress.AddressLine1 = address.AddressLine1;
        existingAddress.AddressLine2 = address.AddressLine2;
        existingAddress.City = address.City;
        existingAddress.StateProvince = address.StateProvince;
        existingAddress.PostalCode = address.PostalCode;
        existingAddress.CountryCode = address.CountryCode;
        existingAddress.UpdatedAt = DateTime.UtcNow;

        if (address.IsDefault && !existingAddress.IsDefault && existingAddress.UserId.HasValue)
        {
            // Clear other default addresses
            var otherDefaults = await _context.Addresses
                .Where(a => a.UserId == existingAddress.UserId && a.Id != existingAddress.Id && a.IsDefault)
                .ToListAsync();

            foreach (var other in otherDefaults)
            {
                other.IsDefault = false;
            }

            existingAddress.IsDefault = true;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated address {AddressId}", address.Id);

        return existingAddress;
    }

    /// <inheritdoc />
    public async Task DeleteAddressAsync(int addressId)
    {
        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId);

        if (address == null)
        {
            throw new InvalidOperationException("Address not found.");
        }

        // Check if address is used in any orders
        var isUsedInOrders = await _context.Orders
            .AnyAsync(o => o.DeliveryAddressId == addressId);

        if (isUsedInOrders)
        {
            throw new InvalidOperationException("Cannot delete address that is associated with existing orders.");
        }

        _context.Addresses.Remove(address);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted address {AddressId}", addressId);
    }

    /// <inheritdoc />
    public async Task SetDefaultAddressAsync(int userId, int addressId)
    {
        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

        if (address == null)
        {
            throw new InvalidOperationException("Address not found.");
        }

        // Clear other default addresses
        var otherDefaults = await _context.Addresses
            .Where(a => a.UserId == userId && a.Id != addressId && a.IsDefault)
            .ToListAsync();

        foreach (var other in otherDefaults)
        {
            other.IsDefault = false;
        }

        address.IsDefault = true;
        address.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Set address {AddressId} as default for user {UserId}", 
            addressId, userId);
    }

    /// <inheritdoc />
    public Task<bool> IsShippingAllowedToCountryAsync(string countryCode)
    {
        return Task.FromResult(AllowedCountries.Contains(countryCode.ToUpperInvariant()));
    }
}
