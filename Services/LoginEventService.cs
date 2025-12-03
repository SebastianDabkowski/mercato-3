using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Data required to log a login event.
/// </summary>
public class LoginEventData
{
    public int? UserId { get; set; }
    public string? Email { get; set; }
    public LoginEventType EventType { get; set; }
    public bool IsSuccessful { get; set; }
    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? SessionToken { get; set; }
}

/// <summary>
/// Result of a security alert check.
/// </summary>
public class SecurityAlertResult
{
    public bool ShouldAlert { get; set; }
    public string? AlertReason { get; set; }
    public SecurityAlertType AlertType { get; set; }
}

/// <summary>
/// Types of security alerts that can be triggered.
/// </summary>
public enum SecurityAlertType
{
    None,
    NewLocation,
    NewDevice,
    MultipleFailedAttempts,
    UnusualTime,
    SuspiciousActivity
}

/// <summary>
/// Interface for login event logging service.
/// </summary>
public interface ILoginEventService
{
    /// <summary>
    /// Logs a login event for security auditing.
    /// </summary>
    /// <param name="data">The login event data.</param>
    /// <returns>The created login event.</returns>
    Task<LoginEvent> LogEventAsync(LoginEventData data);

    /// <summary>
    /// Gets the login history for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="limit">Maximum number of events to return.</param>
    /// <returns>List of login events.</returns>
    Task<List<LoginEvent>> GetUserLoginHistoryAsync(int userId, int limit = 50);

    /// <summary>
    /// Checks if the current login attempt should trigger a security alert.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ipAddress">The IP address of the current attempt.</param>
    /// <param name="userAgent">The user agent of the current attempt.</param>
    /// <returns>Security alert result.</returns>
    Task<SecurityAlertResult> CheckForSecurityAlertAsync(int userId, string? ipAddress, string? userAgent);

    /// <summary>
    /// Cleans up old login events based on retention policy.
    /// </summary>
    /// <param name="retentionDays">Number of days to retain events.</param>
    /// <returns>Number of events deleted.</returns>
    Task<int> CleanupOldEventsAsync(int retentionDays = 90);

    /// <summary>
    /// Gets count of failed login attempts for a user within a time window.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="windowMinutes">Time window in minutes.</param>
    /// <returns>Count of failed attempts.</returns>
    Task<int> GetRecentFailedAttemptsAsync(int userId, int windowMinutes = 60);
}

/// <summary>
/// Service for logging and managing login events.
/// Supports security auditing, login history, and unusual activity detection.
/// </summary>
public class LoginEventService : ILoginEventService
{
    private const int MaxFailedAttemptsForAlert = 3;
    private const int AlertWindowMinutes = 60;
    private const int MaxFailedAttemptsForIncident = 5;
    private const int IncidentWindowMinutes = 10;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<LoginEventService> _logger;
    private readonly ISecurityIncidentService? _securityIncidentService;

    public LoginEventService(
        ApplicationDbContext context,
        ILogger<LoginEventService> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _logger = logger;
        // Use service provider to avoid circular dependency
        _securityIncidentService = serviceProvider.GetService<ISecurityIncidentService>();
    }

    /// <inheritdoc />
    public async Task<LoginEvent> LogEventAsync(LoginEventData data)
    {
        var loginEvent = new LoginEvent
        {
            UserId = data.UserId,
            Email = data.Email,
            EventType = data.EventType,
            IsSuccessful = data.IsSuccessful,
            FailureReason = data.FailureReason,
            IpAddress = data.IpAddress,
            UserAgent = data.UserAgent,
            SessionToken = data.SessionToken,
            CreatedAt = DateTime.UtcNow
        };

        // Check for security alerts on successful logins
        if (data.IsSuccessful && data.UserId.HasValue)
        {
            var alertResult = await CheckForSecurityAlertAsync(data.UserId.Value, data.IpAddress, data.UserAgent);
            loginEvent.TriggeredSecurityAlert = alertResult.ShouldAlert;

            if (alertResult.ShouldAlert)
            {
                _logger.LogWarning(
                    "Security alert triggered for user {UserId}: {AlertReason}",
                    data.UserId,
                    alertResult.AlertReason);
                
                // Create security incident for suspicious login
                await CreateSecurityIncidentIfNeededAsync(data, alertResult);
            }
        }
        // Check for multiple failed login attempts
        else if (!data.IsSuccessful && data.UserId.HasValue)
        {
            var failedAttempts = await GetRecentFailedAttemptsAsync(data.UserId.Value, IncidentWindowMinutes);
            
            // Create security incident if threshold exceeded
            if (failedAttempts >= MaxFailedAttemptsForIncident - 1) // -1 because this attempt hasn't been saved yet
            {
                await CreateMultipleFailedLoginsIncidentAsync(data, failedAttempts + 1);
            }
        }

        _context.LoginEvents.Add(loginEvent);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Login event logged: Type={EventType}, Success={IsSuccessful}, UserId={UserId}, IP={IpAddress}",
            data.EventType,
            data.IsSuccessful,
            data.UserId,
            data.IpAddress);

        return loginEvent;
    }

    /// <inheritdoc />
    public async Task<List<LoginEvent>> GetUserLoginHistoryAsync(int userId, int limit = 50)
    {
        return await _context.LoginEvents
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<SecurityAlertResult> CheckForSecurityAlertAsync(int userId, string? ipAddress, string? userAgent)
    {
        // Check for multiple failed attempts
        var failedAttempts = await GetRecentFailedAttemptsAsync(userId, AlertWindowMinutes);
        if (failedAttempts >= MaxFailedAttemptsForAlert)
        {
            return new SecurityAlertResult
            {
                ShouldAlert = true,
                AlertReason = $"Multiple failed login attempts detected ({failedAttempts} in the last hour)",
                AlertType = SecurityAlertType.MultipleFailedAttempts
            };
        }

        // Pre-check if user has any previous successful logins (used by both IP and device checks)
        bool? hasPreviousLogins = null;

        // Check for new IP address (if we have IP address)
        if (!string.IsNullOrEmpty(ipAddress))
        {
            var hasSeenIp = await _context.LoginEvents
                .Where(e => e.UserId == userId && e.IsSuccessful && e.IpAddress == ipAddress)
                .AnyAsync();

            if (!hasSeenIp)
            {
                // Check if user has any previous successful logins (cached for reuse)
                hasPreviousLogins ??= await _context.LoginEvents
                    .Where(e => e.UserId == userId && e.IsSuccessful)
                    .AnyAsync();

                if (hasPreviousLogins.Value)
                {
                    return new SecurityAlertResult
                    {
                        ShouldAlert = true,
                        AlertReason = $"Login from new IP address: {ipAddress}",
                        AlertType = SecurityAlertType.NewLocation
                    };
                }
            }
        }

        // Check for new device (user agent)
        if (!string.IsNullOrEmpty(userAgent))
        {
            var hasSeenDevice = await _context.LoginEvents
                .Where(e => e.UserId == userId && e.IsSuccessful && e.UserAgent == userAgent)
                .AnyAsync();

            if (!hasSeenDevice)
            {
                // Reuse cached value or fetch if not already retrieved
                hasPreviousLogins ??= await _context.LoginEvents
                    .Where(e => e.UserId == userId && e.IsSuccessful)
                    .AnyAsync();

                if (hasPreviousLogins.Value)
                {
                    return new SecurityAlertResult
                    {
                        ShouldAlert = true,
                        AlertReason = "Login from new device detected",
                        AlertType = SecurityAlertType.NewDevice
                    };
                }
            }
        }

        return new SecurityAlertResult
        {
            ShouldAlert = false,
            AlertType = SecurityAlertType.None
        };
    }

    /// <inheritdoc />
    public async Task<int> CleanupOldEventsAsync(int retentionDays = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        const int batchSize = 1000;
        var totalDeleted = 0;
        
        // Process in batches to avoid memory issues with large datasets
        while (true)
        {
            var batch = await _context.LoginEvents
                .Where(e => e.CreatedAt < cutoffDate)
                .Take(batchSize)
                .ToListAsync();

            if (batch.Count == 0)
            {
                break;
            }

            _context.LoginEvents.RemoveRange(batch);
            await _context.SaveChangesAsync();
            totalDeleted += batch.Count;

            // If we got fewer than batch size, we're done
            if (batch.Count < batchSize)
            {
                break;
            }
        }

        if (totalDeleted > 0)
        {
            _logger.LogInformation(
                "Cleaned up {Count} login events older than {RetentionDays} days",
                totalDeleted,
                retentionDays);
        }

        return totalDeleted;
    }

    /// <inheritdoc />
    public async Task<int> GetRecentFailedAttemptsAsync(int userId, int windowMinutes = 60)
    {
        var windowStart = DateTime.UtcNow.AddMinutes(-windowMinutes);
        
        return await _context.LoginEvents
            .Where(e => e.UserId == userId && 
                       !e.IsSuccessful && 
                       e.CreatedAt >= windowStart)
            .CountAsync();
    }

    /// <summary>
    /// Creates a security incident for suspicious login activity.
    /// </summary>
    private async Task CreateSecurityIncidentIfNeededAsync(LoginEventData data, SecurityAlertResult alertResult)
    {
        if (_securityIncidentService == null)
        {
            _logger.LogWarning("SecurityIncidentService not available. Skipping incident creation.");
            return;
        }

        try
        {
            var incidentType = alertResult.AlertType switch
            {
                SecurityAlertType.NewLocation => SecurityIncidentType.SuspectedAccountCompromise,
                SecurityAlertType.NewDevice => SecurityIncidentType.SuspectedAccountCompromise,
                SecurityAlertType.MultipleFailedAttempts => SecurityIncidentType.MultipleFailedLogins,
                _ => SecurityIncidentType.Other
            };

            var incidentData = new CreateSecurityIncidentData
            {
                IncidentType = incidentType,
                Severity = SecurityIncidentSeverity.Medium,
                DetectionRule = alertResult.AlertReason ?? "Suspicious login activity detected",
                Source = data.IpAddress,
                UserId = data.UserId,
                Details = $"Login event type: {data.EventType}, Email: {data.Email}, User Agent: {data.UserAgent}",
                Metadata = $"{{\"alertType\":\"{alertResult.AlertType}\",\"eventType\":\"{data.EventType}\"}}",
                DetectedAt = DateTime.UtcNow
            };

            await _securityIncidentService.CreateIncidentAsync(incidentData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create security incident for login event");
        }
    }

    /// <summary>
    /// Creates a security incident for multiple failed login attempts.
    /// </summary>
    private async Task CreateMultipleFailedLoginsIncidentAsync(LoginEventData data, int attemptCount)
    {
        if (_securityIncidentService == null)
        {
            _logger.LogWarning("SecurityIncidentService not available. Skipping incident creation.");
            return;
        }

        try
        {
            var incidentData = new CreateSecurityIncidentData
            {
                IncidentType = SecurityIncidentType.MultipleFailedLogins,
                Severity = SecurityIncidentSeverity.High,
                DetectionRule = $"Multiple failed login attempts ({attemptCount} attempts in {IncidentWindowMinutes} minutes)",
                Source = data.IpAddress,
                UserId = data.UserId,
                Details = $"Email: {data.Email}, Failed attempts: {attemptCount}, User Agent: {data.UserAgent}, Last failure reason: {data.FailureReason}",
                Metadata = $"{{\"attemptCount\":{attemptCount},\"windowMinutes\":{IncidentWindowMinutes},\"eventType\":\"{data.EventType}\"}}",
                DetectedAt = DateTime.UtcNow
            };

            await _securityIncidentService.CreateIncidentAsync(incidentData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create security incident for multiple failed login attempts");
        }
    }
}

