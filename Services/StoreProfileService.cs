using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Result of a store profile operation.
/// </summary>
public class StoreProfileResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public Store? Store { get; set; }
}

/// <summary>
/// Data for updating the store profile.
/// </summary>
public class UpdateStoreProfileData
{
    public required string StoreName { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? ContactEmail { get; set; }
    public string? PhoneNumber { get; set; }
    public string? WebsiteUrl { get; set; }
}

/// <summary>
/// Interface for store profile service.
/// </summary>
public interface IStoreProfileService
{
    /// <summary>
    /// Gets the store for a user.
    /// </summary>
    Task<Store?> GetStoreAsync(int userId);

    /// <summary>
    /// Updates the store profile.
    /// </summary>
    Task<StoreProfileResult> UpdateStoreProfileAsync(int userId, UpdateStoreProfileData data);

    /// <summary>
    /// Updates the store logo.
    /// </summary>
    Task<StoreProfileResult> UpdateStoreLogoAsync(int userId, string logoUrl);

    /// <summary>
    /// Gets a store by its ID for public viewing.
    /// </summary>
    Task<Store?> GetStoreByIdAsync(int storeId);

    /// <summary>
    /// Checks if a store name is unique (excluding the current store).
    /// </summary>
    Task<bool> IsStoreNameUniqueAsync(string storeName, int? excludeStoreId = null);
}

/// <summary>
/// Service for managing store profiles.
/// </summary>
public class StoreProfileService : IStoreProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StoreProfileService> _logger;

    // Allowed image extensions
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    // Maximum file size (5 MB)
    public const long MaxLogoFileSizeBytes = 5 * 1024 * 1024;

    public StoreProfileService(
        ApplicationDbContext context,
        ILogger<StoreProfileService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Store?> GetStoreAsync(int userId)
    {
        return await _context.Stores
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    /// <inheritdoc />
    public async Task<Store?> GetStoreByIdAsync(int storeId)
    {
        return await _context.Stores
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == storeId && s.Status == StoreStatus.Active);
    }

    /// <inheritdoc />
    public async Task<bool> IsStoreNameUniqueAsync(string storeName, int? excludeStoreId = null)
    {
        var normalizedName = storeName.Trim().ToLowerInvariant();
        
        var query = _context.Stores.AsQueryable();
        
        if (excludeStoreId.HasValue)
        {
            query = query.Where(s => s.Id != excludeStoreId.Value);
        }

        return !await query.AnyAsync(s => s.StoreName.ToLowerInvariant() == normalizedName);
    }

    /// <inheritdoc />
    public async Task<StoreProfileResult> UpdateStoreProfileAsync(int userId, UpdateStoreProfileData data)
    {
        var result = new StoreProfileResult();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(data.StoreName))
        {
            result.Errors.Add("Store name is required.");
        }
        else if (data.StoreName.Length > 100)
        {
            result.Errors.Add("Store name must be 100 characters or less.");
        }

        if (data.Description?.Length > 1000)
        {
            result.Errors.Add("Description must be 1000 characters or less.");
        }

        if (data.Category?.Length > 100)
        {
            result.Errors.Add("Category must be 100 characters or less.");
        }

        // Validate email format
        if (!string.IsNullOrWhiteSpace(data.ContactEmail))
        {
            if (data.ContactEmail.Length > 256)
            {
                result.Errors.Add("Contact email must be 256 characters or less.");
            }
            else if (!IsValidEmail(data.ContactEmail))
            {
                result.Errors.Add("Please enter a valid email address.");
            }
        }

        // Validate phone number
        if (!string.IsNullOrWhiteSpace(data.PhoneNumber) && data.PhoneNumber.Length > 20)
        {
            result.Errors.Add("Phone number must be 20 characters or less.");
        }

        // Validate website URL
        if (!string.IsNullOrWhiteSpace(data.WebsiteUrl))
        {
            if (data.WebsiteUrl.Length > 500)
            {
                result.Errors.Add("Website URL must be 500 characters or less.");
            }
            else if (!IsValidUrl(data.WebsiteUrl))
            {
                result.Errors.Add("Please enter a valid website URL (must start with http:// or https://).");
            }
        }

        if (result.Errors.Count > 0)
        {
            return result;
        }

        // Get the store
        var store = await _context.Stores.FirstOrDefaultAsync(s => s.UserId == userId);
        if (store == null)
        {
            result.Errors.Add("Store not found.");
            return result;
        }

        // Check store name uniqueness
        if (!await IsStoreNameUniqueAsync(data.StoreName, store.Id))
        {
            result.Errors.Add("A store with this name already exists. Please choose a different name.");
            return result;
        }

        // Update the store
        store.StoreName = data.StoreName.Trim();
        store.Description = data.Description?.Trim();
        store.Category = data.Category?.Trim();
        store.ContactEmail = data.ContactEmail?.Trim();
        store.PhoneNumber = data.PhoneNumber?.Trim();
        store.WebsiteUrl = data.WebsiteUrl?.Trim();
        store.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated store profile for user {UserId}, store {StoreId}", userId, store.Id);

        result.Success = true;
        result.Store = store;
        return result;
    }

    /// <inheritdoc />
    public async Task<StoreProfileResult> UpdateStoreLogoAsync(int userId, string logoUrl)
    {
        var result = new StoreProfileResult();

        if (string.IsNullOrWhiteSpace(logoUrl))
        {
            result.Errors.Add("Logo URL is required.");
            return result;
        }

        if (logoUrl.Length > 500)
        {
            result.Errors.Add("Logo URL must be 500 characters or less.");
            return result;
        }

        var store = await _context.Stores.FirstOrDefaultAsync(s => s.UserId == userId);
        if (store == null)
        {
            result.Errors.Add("Store not found.");
            return result;
        }

        store.LogoUrl = logoUrl;
        store.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated store logo for user {UserId}, store {StoreId}", userId, store.Id);

        result.Success = true;
        result.Store = store;
        return result;
    }

    /// <summary>
    /// Validates if a file extension is allowed for logo uploads.
    /// </summary>
    public static bool IsAllowedImageExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(extension) && AllowedImageExtensions.Contains(extension);
    }

    /// <summary>
    /// Validates if the file size is within the allowed limit.
    /// </summary>
    public static bool IsAllowedFileSize(long fileSize)
    {
        return fileSize > 0 && fileSize <= MaxLogoFileSizeBytes;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
