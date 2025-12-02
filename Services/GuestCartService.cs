using Microsoft.AspNetCore.Http;

namespace MercatoApp.Services;

/// <summary>
/// Interface for managing guest cart identifiers.
/// </summary>
public interface IGuestCartService
{
    /// <summary>
    /// Gets or creates a persistent guest cart identifier.
    /// </summary>
    /// <returns>The guest cart identifier.</returns>
    string GetOrCreateGuestCartId();

    /// <summary>
    /// Gets the guest cart identifier from the request cookie if it exists.
    /// </summary>
    /// <returns>The guest cart identifier or null if not found.</returns>
    string? GetGuestCartIdIfExists();

    /// <summary>
    /// Clears the guest cart identifier cookie.
    /// </summary>
    void ClearGuestCartId();
}

/// <summary>
/// Service for managing guest cart identifiers using cookies.
/// </summary>
public class GuestCartService : IGuestCartService
{
    private const string GuestCartCookieName = "MercatoGuestCart";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GuestCartService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public string GetOrCreateGuestCartId()
    {
        var httpContext = GetHttpContext();

        // Try to get existing guest cart ID from cookie
        var guestCartId = httpContext.Request.Cookies[GuestCartCookieName];

        if (string.IsNullOrEmpty(guestCartId))
        {
            // Generate a new unique identifier
            guestCartId = Guid.NewGuid().ToString();

            // Store in a persistent cookie (7 days)
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                // Use secure cookies when connection is HTTPS
                Secure = httpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                IsEssential = true // Required for cart functionality
            };

            httpContext.Response.Cookies.Append(GuestCartCookieName, guestCartId, cookieOptions);
        }

        return guestCartId;
    }

    /// <inheritdoc />
    public string? GetGuestCartIdIfExists()
    {
        var httpContext = GetHttpContext();
        return httpContext.Request.Cookies[GuestCartCookieName];
    }

    /// <inheritdoc />
    public void ClearGuestCartId()
    {
        var httpContext = GetHttpContext();
        httpContext.Response.Cookies.Delete(GuestCartCookieName);
    }

    private HttpContext GetHttpContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new InvalidOperationException("HttpContext is not available.");
        }
        return httpContext;
    }
}
