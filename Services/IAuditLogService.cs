using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for audit logging service.
/// Handles logging of critical actions for security, compliance, and auditing purposes.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Logs a critical action in the system.
    /// </summary>
    /// <param name="userId">The user ID performing the action (null for system actions).</param>
    /// <param name="actionType">The type of action being performed.</param>
    /// <param name="entityType">The type of entity being acted upon.</param>
    /// <param name="entityId">The ID of the entity being acted upon.</param>
    /// <param name="entityDisplayName">Display name for the entity.</param>
    /// <param name="targetUserId">Optional target user ID for user-related actions.</param>
    /// <param name="success">Whether the action was successful.</param>
    /// <param name="failureReason">Reason for failure if not successful.</param>
    /// <param name="details">Additional details about the action.</param>
    /// <param name="previousValue">Previous state/value before the action.</param>
    /// <param name="newValue">New state/value after the action.</param>
    /// <param name="ipAddress">IP address from which the action was performed.</param>
    /// <param name="userAgent">User agent string from the request.</param>
    /// <param name="correlationId">Correlation ID for grouping related entries.</param>
    /// <returns>The created audit log entry.</returns>
    Task<AuditLog> LogActionAsync(
        int? userId,
        AuditActionType actionType,
        string entityType,
        int? entityId,
        string? entityDisplayName,
        int? targetUserId = null,
        bool success = true,
        string? failureReason = null,
        string? details = null,
        string? previousValue = null,
        string? newValue = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? correlationId = null);

    /// <summary>
    /// Gets a paginated list of audit log entries based on filter criteria.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <returns>A paginated list of audit log entries.</returns>
    Task<PaginatedList<AuditLog>> GetAuditLogsAsync(AuditLogFilter filter);

    /// <summary>
    /// Gets a specific audit log entry by ID.
    /// </summary>
    /// <param name="id">The audit log entry ID.</param>
    /// <returns>The audit log entry, or null if not found.</returns>
    Task<AuditLog?> GetAuditLogByIdAsync(int id);

    /// <summary>
    /// Gets audit log entries for a specific entity.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="limit">Maximum number of entries to return.</param>
    /// <returns>A list of audit log entries.</returns>
    Task<List<AuditLog>> GetEntityAuditLogsAsync(string entityType, int entityId, int limit = 50);

    /// <summary>
    /// Gets available entity types that have audit logs.
    /// </summary>
    /// <returns>A list of distinct entity types.</returns>
    Task<List<string>> GetEntityTypesAsync();

    /// <summary>
    /// Archives audit logs older than the specified retention period.
    /// </summary>
    /// <param name="retentionDays">Number of days to retain non-archived logs.</param>
    /// <returns>Number of logs archived.</returns>
    Task<int> ArchiveOldLogsAsync(int retentionDays);

    /// <summary>
    /// Deletes archived audit logs older than the specified retention period.
    /// </summary>
    /// <param name="retentionDays">Number of days to retain archived logs.</param>
    /// <returns>Number of logs deleted.</returns>
    Task<int> DeleteArchivedLogsAsync(int retentionDays);

    /// <summary>
    /// Verifies the integrity of audit log entries by checking their hashes.
    /// </summary>
    /// <param name="startDate">Start date for verification range.</param>
    /// <param name="endDate">End date for verification range.</param>
    /// <returns>A tuple with the number of entries checked and number of tampered entries found.</returns>
    Task<(int checkedCount, int tamperedCount)> VerifyIntegrityAsync(DateTime? startDate = null, DateTime? endDate = null);
}
