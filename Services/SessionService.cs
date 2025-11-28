using System.Security.Cryptography;
using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Result of a session creation attempt.
/// </summary>
public class SessionCreationResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public UserSession? Session { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of a session validation attempt.
/// </summary>
public class SessionValidationResult
{
    public bool IsValid { get; set; }
    public User? User { get; set; }
    public UserSession? Session { get; set; }
    public bool RequiresReauthentication { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Data required for session creation.
/// </summary>
public class SessionCreationData
{
    public required int UserId { get; set; }
    public string? SecurityStamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsPersistent { get; set; }
}

/// <summary>
/// Interface for session management service.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Creates a new session for a user.
    /// </summary>
    /// <param name="data">The session creation data.</param>
    /// <returns>The session creation result with the secure token.</returns>
    Task<SessionCreationResult> CreateSessionAsync(SessionCreationData data);

    /// <summary>
    /// Validates a session token.
    /// </summary>
    /// <param name="token">The session token to validate.</param>
    /// <returns>The validation result including user info if valid.</returns>
    Task<SessionValidationResult> ValidateSessionAsync(string token);

    /// <summary>
    /// Invalidates a session (logout).
    /// </summary>
    /// <param name="token">The session token to invalidate.</param>
    /// <returns>True if the session was successfully invalidated.</returns>
    Task<bool> InvalidateSessionAsync(string token);

    /// <summary>
    /// Invalidates all sessions for a user.
    /// </summary>
    /// <param name="userId">The user ID whose sessions should be invalidated.</param>
    /// <returns>The number of sessions invalidated.</returns>
    Task<int> InvalidateAllUserSessionsAsync(int userId);

    /// <summary>
    /// Updates the last accessed time for a session (sliding expiration).
    /// </summary>
    /// <param name="token">The session token.</param>
    Task UpdateSessionAccessTimeAsync(string token);

    /// <summary>
    /// Cleans up expired sessions from the database.
    /// </summary>
    /// <returns>The number of sessions removed.</returns>
    Task<int> CleanupExpiredSessionsAsync();

    /// <summary>
    /// Gets the session expiry duration for persistent sessions.
    /// </summary>
    TimeSpan PersistentSessionDuration { get; }

    /// <summary>
    /// Gets the session expiry duration for non-persistent sessions.
    /// </summary>
    TimeSpan SessionDuration { get; }
}

/// <summary>
/// Service for managing user sessions with secure tokens.
/// Sessions are stored in the database to support horizontal scaling.
/// </summary>
public class SessionService : ISessionService
{
    private const int TokenSizeBytes = 32;
    
    // Default session durations
    private static readonly TimeSpan DefaultSessionDuration = TimeSpan.FromHours(2);
    private static readonly TimeSpan DefaultPersistentSessionDuration = TimeSpan.FromDays(7);
    private static readonly TimeSpan SlidingExpirationWindow = TimeSpan.FromMinutes(30);

    private readonly ApplicationDbContext _context;
    private readonly ILogger<SessionService> _logger;

    public SessionService(
        ApplicationDbContext context,
        ILogger<SessionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public TimeSpan PersistentSessionDuration => DefaultPersistentSessionDuration;

    /// <inheritdoc />
    public TimeSpan SessionDuration => DefaultSessionDuration;

    /// <inheritdoc />
    public async Task<SessionCreationResult> CreateSessionAsync(SessionCreationData data)
    {
        try
        {
            // Verify user exists
            var user = await _context.Users.FindAsync(data.UserId);
            if (user == null)
            {
                return new SessionCreationResult
                {
                    Success = false,
                    ErrorMessage = "User not found."
                };
            }

            // Generate secure random token
            var token = GenerateSecureToken();
            var sessionDuration = data.IsPersistent ? DefaultPersistentSessionDuration : DefaultSessionDuration;

            var session = new UserSession
            {
                Token = token,
                UserId = data.UserId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(sessionDuration),
                LastAccessedAt = DateTime.UtcNow,
                IsValid = true,
                IpAddress = data.IpAddress,
                UserAgent = data.UserAgent,
                SecurityStamp = data.SecurityStamp ?? user.SecurityStamp,
                IsPersistent = data.IsPersistent
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Session created for user {UserId}. Expires at {ExpiresAt}. Persistent: {IsPersistent}",
                data.UserId,
                session.ExpiresAt,
                data.IsPersistent);

            return new SessionCreationResult
            {
                Success = true,
                Token = token,
                Session = session
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create session for user {UserId}", data.UserId);
            return new SessionCreationResult
            {
                Success = false,
                ErrorMessage = "Failed to create session."
            };
        }
    }

    /// <inheritdoc />
    public async Task<SessionValidationResult> ValidateSessionAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new SessionValidationResult
            {
                IsValid = false,
                Reason = "Token is empty."
            };
        }

        var session = await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Token == token);

        if (session == null)
        {
            return new SessionValidationResult
            {
                IsValid = false,
                Reason = "Session not found."
            };
        }

        // Check if session is still marked as valid
        if (!session.IsValid)
        {
            return new SessionValidationResult
            {
                IsValid = false,
                RequiresReauthentication = true,
                Reason = "Session has been invalidated."
            };
        }

        // Check if session has expired
        if (session.ExpiresAt < DateTime.UtcNow)
        {
            // Mark session as invalid
            session.IsValid = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Session expired for user {UserId}", session.UserId);

            return new SessionValidationResult
            {
                IsValid = false,
                RequiresReauthentication = true,
                Reason = "Session has expired."
            };
        }

        // Check if user's security stamp has changed (password change, etc.)
        if (session.User != null && 
            !string.IsNullOrEmpty(session.SecurityStamp) && 
            session.User.SecurityStamp != session.SecurityStamp)
        {
            // Security stamp mismatch - invalidate session
            session.IsValid = false;
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Session invalidated due to security stamp mismatch for user {UserId}",
                session.UserId);

            return new SessionValidationResult
            {
                IsValid = false,
                RequiresReauthentication = true,
                Reason = "Security credentials have changed."
            };
        }

        // Check if user account is still active
        if (session.User != null && session.User.Status == AccountStatus.Suspended)
        {
            session.IsValid = false;
            await _context.SaveChangesAsync();

            return new SessionValidationResult
            {
                IsValid = false,
                RequiresReauthentication = true,
                Reason = "Account has been suspended."
            };
        }

        return new SessionValidationResult
        {
            IsValid = true,
            User = session.User,
            Session = session
        };
    }

    /// <inheritdoc />
    public async Task<bool> InvalidateSessionAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.Token == token);
        if (session == null)
        {
            return false;
        }

        session.IsValid = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Session invalidated for user {UserId}", session.UserId);

        return true;
    }

    /// <inheritdoc />
    public async Task<int> InvalidateAllUserSessionsAsync(int userId)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsValid)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsValid = false;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("All {Count} sessions invalidated for user {UserId}", sessions.Count, userId);

        return sessions.Count;
    }

    /// <inheritdoc />
    public async Task UpdateSessionAccessTimeAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.Token == token && s.IsValid);
        if (session == null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        // Only update if enough time has passed to reduce database writes
        if (now - session.LastAccessedAt > TimeSpan.FromMinutes(5))
        {
            session.LastAccessedAt = now;

            // Implement sliding expiration - extend session if within window
            var timeUntilExpiry = session.ExpiresAt - now;
            if (timeUntilExpiry < SlidingExpirationWindow && session.IsPersistent)
            {
                session.ExpiresAt = now.Add(DefaultPersistentSessionDuration);
                _logger.LogDebug("Session expiration extended for user {UserId}", session.UserId);
            }
            else if (timeUntilExpiry < SlidingExpirationWindow && !session.IsPersistent)
            {
                session.ExpiresAt = now.Add(DefaultSessionDuration);
                _logger.LogDebug("Session expiration extended for user {UserId}", session.UserId);
            }

            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredSessionsAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-1); // Keep expired sessions for 1 day for audit
        var expiredSessions = await _context.UserSessions
            .Where(s => !s.IsValid && s.ExpiresAt < cutoff)
            .ToListAsync();

        _context.UserSessions.RemoveRange(expiredSessions);
        await _context.SaveChangesAsync();

        if (expiredSessions.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count);
        }

        return expiredSessions.Count;
    }

    private static string GenerateSecureToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(TokenSizeBytes);
        return Convert.ToBase64String(tokenBytes);
    }
}
