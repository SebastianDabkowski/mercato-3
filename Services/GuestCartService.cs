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
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new InvalidOperationException("HttpContext is not available.");
        }

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
                Secure = true, // HTTPS only for security
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                IsEssential = true // Required for cart functionality
            };

            httpContext.Response.Cookies.Append(GuestCartCookieName, guestCartId, cookieOptions);
        }

        return guestCartId;
    }

    /// <inheritdoc />
    public void ClearGuestCartId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new InvalidOperationException("HttpContext is not available.");
        }

        httpContext.Response.Cookies.Delete(GuestCartCookieName);
    }
}
