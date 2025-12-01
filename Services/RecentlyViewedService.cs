using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MercatoApp.Services;

/// <summary>
/// Data for a recently viewed product.
/// </summary>
public class RecentlyViewedProductData
{
    public int ProductId { get; set; }
    public DateTime ViewedAt { get; set; }
}

/// <summary>
/// Interface for recently viewed products service.
/// </summary>
public interface IRecentlyViewedService
{
    /// <summary>
    /// Tracks a product view.
    /// </summary>
    /// <param name="productId">The product ID to track.</param>
    void TrackProductView(int productId);

    /// <summary>
    /// Gets the list of recently viewed product IDs.
    /// </summary>
    /// <returns>List of product IDs ordered from most recent to oldest.</returns>
    List<int> GetRecentlyViewedProductIds();

    /// <summary>
    /// Gets the list of recently viewed products with details.
    /// Only returns active products.
    /// </summary>
    /// <param name="maxItems">Maximum number of items to return.</param>
    /// <returns>List of products ordered from most recent to oldest.</returns>
    Task<List<Product>> GetRecentlyViewedProductsAsync(int maxItems = 10);

    /// <summary>
    /// Clears the recently viewed list.
    /// </summary>
    void ClearRecentlyViewed();
}

/// <summary>
/// Service for tracking recently viewed products using cookies.
/// </summary>
public class RecentlyViewedService : IRecentlyViewedService
{
    private const string CookieName = "MercatoRecentlyViewed";
    private const int DefaultMaxItems = 10;
    private const int CookieExpirationDays = 30;

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RecentlyViewedService> _logger;

    public RecentlyViewedService(
        IHttpContextAccessor httpContextAccessor,
        ApplicationDbContext context,
        ILogger<RecentlyViewedService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public void TrackProductView(int productId)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogWarning("HttpContext is null, cannot track product view");
                return;
            }

            var recentlyViewed = GetRecentlyViewedData();

            // Remove existing entry if product was already viewed (to move it to front)
            recentlyViewed.RemoveAll(p => p.ProductId == productId);

            // Add new entry at the front
            recentlyViewed.Insert(0, new RecentlyViewedProductData
            {
                ProductId = productId,
                ViewedAt = DateTime.UtcNow
            });

            // Keep only the configured maximum number of items
            if (recentlyViewed.Count > DefaultMaxItems)
            {
                recentlyViewed = recentlyViewed.Take(DefaultMaxItems).ToList();
            }

            // Store back in cookie
            SaveRecentlyViewedData(recentlyViewed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking product view for product {ProductId}", productId);
        }
    }

    /// <inheritdoc />
    public List<int> GetRecentlyViewedProductIds()
    {
        try
        {
            var recentlyViewed = GetRecentlyViewedData();
            return recentlyViewed.Select(p => p.ProductId).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recently viewed product IDs");
            return new List<int>();
        }
    }

    /// <inheritdoc />
    public async Task<List<Product>> GetRecentlyViewedProductsAsync(int maxItems = 10)
    {
        try
        {
            var productIds = GetRecentlyViewedProductIds();
            if (productIds.Count == 0)
            {
                return new List<Product>();
            }

            // Fetch products from database, filtering for only active products
            var products = await _context.Products
                .Include(p => p.Store)
                .Where(p => productIds.Contains(p.Id) && p.Status == ProductStatus.Active)
                .ToListAsync();

            // Order by the original viewing order
            var orderedProducts = productIds
                .Select(id => products.FirstOrDefault(p => p.Id == id))
                .Where(p => p != null)
                .Cast<Product>()
                .Take(maxItems)
                .ToList();

            return orderedProducts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recently viewed products");
            return new List<Product>();
        }
    }

    /// <inheritdoc />
    public void ClearRecentlyViewed()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogWarning("HttpContext is null, cannot clear recently viewed");
                return;
            }

            httpContext.Response.Cookies.Delete(CookieName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing recently viewed products");
        }
    }

    /// <summary>
    /// Gets the recently viewed data from the cookie.
    /// </summary>
    private List<RecentlyViewedProductData> GetRecentlyViewedData()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return new List<RecentlyViewedProductData>();
        }

        var cookieValue = httpContext.Request.Cookies[CookieName];
        if (string.IsNullOrEmpty(cookieValue))
        {
            return new List<RecentlyViewedProductData>();
        }

        try
        {
            var data = JsonSerializer.Deserialize<List<RecentlyViewedProductData>>(cookieValue);
            return data ?? new List<RecentlyViewedProductData>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize recently viewed cookie, returning empty list");
            return new List<RecentlyViewedProductData>();
        }
    }

    /// <summary>
    /// Saves the recently viewed data to the cookie.
    /// </summary>
    private void SaveRecentlyViewedData(List<RecentlyViewedProductData> data)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(data);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = false, // Allow JavaScript to read if needed
                Secure = httpContext.Request.IsHttps, // HTTPS only in production
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(CookieExpirationDays),
                IsEssential = false // Not essential for GDPR compliance
            };

            httpContext.Response.Cookies.Append(CookieName, json, cookieOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save recently viewed cookie");
        }
    }
}
