using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for admin audit log service.
/// </summary>
public interface IAdminAuditLogService
{
    /// <summary>
    /// Gets a paginated list of audit log entries based on filter criteria.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <returns>A paginated list of audit log entries.</returns>
    Task<PaginatedList<AdminAuditLog>> GetAuditLogsAsync(AdminAuditLogFilter filter);

    /// <summary>
    /// Gets a specific audit log entry by ID.
    /// </summary>
    /// <param name="id">The audit log entry ID.</param>
    /// <returns>The audit log entry, or null if not found.</returns>
    Task<AdminAuditLog?> GetAuditLogByIdAsync(int id);

    /// <summary>
    /// Gets audit log entries for a specific entity.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="limit">Maximum number of entries to return.</param>
    /// <returns>A list of audit log entries.</returns>
    Task<List<AdminAuditLog>> GetEntityAuditLogsAsync(string entityType, int entityId, int limit = 50);

    /// <summary>
    /// Logs an admin action.
    /// </summary>
    /// <param name="adminUserId">The admin user ID performing the action.</param>
    /// <param name="action">The action performed.</param>
    /// <param name="entityType">The type of entity being acted upon.</param>
    /// <param name="entityId">The ID of the entity being acted upon.</param>
    /// <param name="entityDisplayName">Display name for the entity.</param>
    /// <param name="targetUserId">Optional target user ID for user-related actions.</param>
    /// <param name="reason">Optional reason for the action.</param>
    /// <param name="metadata">Optional metadata about the action.</param>
    /// <returns>The created audit log entry.</returns>
    Task<AdminAuditLog> LogActionAsync(
        int adminUserId,
        string action,
        string entityType,
        int? entityId,
        string? entityDisplayName,
        int? targetUserId = null,
        string? reason = null,
        string? metadata = null);

    /// <summary>
    /// Gets available entity types that have audit logs.
    /// </summary>
    /// <returns>A list of distinct entity types.</returns>
    Task<List<string>> GetEntityTypesAsync();

    /// <summary>
    /// Gets available actions that have been logged.
    /// </summary>
    /// <returns>A list of distinct actions.</returns>
    Task<List<string>> GetActionsAsync();

    /// <summary>
    /// Logs access to sensitive data by admin or support users.
    /// </summary>
    /// <param name="adminUserId">The admin/support user ID accessing the data.</param>
    /// <param name="entityType">The type of sensitive entity being accessed (e.g., "UserProfile", "PayoutDetails").</param>
    /// <param name="entityId">The ID of the entity being accessed.</param>
    /// <param name="entityDisplayName">Display name for the entity (e.g., user email, order number).</param>
    /// <param name="targetUserId">The user ID whose data is being accessed.</param>
    /// <returns>The created audit log entry.</returns>
    Task<AdminAuditLog> LogSensitiveAccessAsync(
        int adminUserId,
        string entityType,
        int entityId,
        string? entityDisplayName,
        int? targetUserId = null);
}
