using MercatoApp.Models;
using System.Text.Json;

namespace MercatoApp.Services;

/// <summary>
/// Helper service for common audit logging operations.
/// Simplifies audit logging for critical actions throughout the application.
/// </summary>
public class AuditHelper
{
    private readonly IAuditLogService _auditLogService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditHelper> _logger;

    public AuditHelper(
        IAuditLogService auditLogService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditHelper> logger)
    {
        _auditLogService = auditLogService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Logs a critical action with context from the current HTTP request.
    /// </summary>
    public async Task<AuditLog> LogActionAsync(
        int? userId,
        AuditActionType actionType,
        string entityType,
        int? entityId,
        string? entityDisplayName,
        int? targetUserId = null,
        bool success = true,
        string? failureReason = null,
        string? details = null,
        object? previousValue = null,
        object? newValue = null,
        string? correlationId = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        string? ipAddress = null;
        string? userAgent = null;

        if (httpContext != null)
        {
            ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault();
        }

        string? previousValueJson = previousValue != null ? JsonSerializer.Serialize(previousValue) : null;
        string? newValueJson = newValue != null ? JsonSerializer.Serialize(newValue) : null;

        try
        {
            return await _auditLogService.LogActionAsync(
                userId,
                actionType,
                entityType,
                entityId,
                entityDisplayName,
                targetUserId,
                success,
                failureReason,
                details,
                previousValueJson,
                newValueJson,
                ipAddress,
                userAgent,
                correlationId);
        }
        catch (Exception ex)
        {
            // Audit logging should never fail the primary operation
            _logger.LogError(ex, 
                "Failed to create audit log for action {ActionType} on {EntityType} {EntityId}",
                actionType, entityType, entityId);
            
            // Return a placeholder entry
            return new AuditLog
            {
                UserId = userId,
                ActionType = actionType,
                EntityType = entityType,
                EntityId = entityId,
                Success = success
            };
        }
    }

    /// <summary>
    /// Logs a login attempt.
    /// </summary>
    public Task<AuditLog> LogLoginAsync(int? userId, string email, bool success, string? failureReason = null)
    {
        return LogActionAsync(
            userId,
            success ? AuditActionType.Login : AuditActionType.LoginFailed,
            "User",
            userId,
            email,
            success: success,
            failureReason: failureReason);
    }

    /// <summary>
    /// Logs a logout action.
    /// </summary>
    public Task<AuditLog> LogLogoutAsync(int userId, string email)
    {
        return LogActionAsync(
            userId,
            AuditActionType.Logout,
            "User",
            userId,
            email);
    }

    /// <summary>
    /// Logs a role assignment.
    /// </summary>
    public Task<AuditLog> LogRoleAssignedAsync(int adminUserId, int targetUserId, string targetEmail, string roleName)
    {
        return LogActionAsync(
            adminUserId,
            AuditActionType.RoleAssigned,
            "User",
            targetUserId,
            targetEmail,
            targetUserId: targetUserId,
            details: $"Role assigned: {roleName}",
            newValue: new { Role = roleName });
    }

    /// <summary>
    /// Logs a role revocation.
    /// </summary>
    public Task<AuditLog> LogRoleRevokedAsync(int adminUserId, int targetUserId, string targetEmail, string roleName)
    {
        return LogActionAsync(
            adminUserId,
            AuditActionType.RoleRevoked,
            "User",
            targetUserId,
            targetEmail,
            targetUserId: targetUserId,
            details: $"Role revoked: {roleName}",
            previousValue: new { Role = roleName });
    }

    /// <summary>
    /// Logs a payout method change.
    /// </summary>
    public Task<AuditLog> LogPayoutMethodChangedAsync(
        int userId,
        int payoutMethodId,
        string action,
        object? previousValue = null,
        object? newValue = null)
    {
        var actionType = action switch
        {
            "Added" => AuditActionType.PayoutMethodAdded,
            "Updated" => AuditActionType.PayoutMethodUpdated,
            "Deleted" => AuditActionType.PayoutMethodDeleted,
            "SetDefault" => AuditActionType.PayoutMethodSetDefault,
            _ => AuditActionType.PayoutMethodUpdated
        };

        return LogActionAsync(
            userId,
            actionType,
            "PayoutMethod",
            payoutMethodId,
            $"Payout Method #{payoutMethodId}",
            previousValue: previousValue,
            newValue: newValue);
    }

    /// <summary>
    /// Logs an order status override.
    /// </summary>
    public Task<AuditLog> LogOrderStatusOverrideAsync(
        int userId,
        int orderId,
        string orderNumber,
        string previousStatus,
        string newStatus,
        string? reason = null)
    {
        return LogActionAsync(
            userId,
            AuditActionType.OrderStatusOverridden,
            "Order",
            orderId,
            orderNumber,
            details: reason,
            previousValue: new { Status = previousStatus },
            newValue: new { Status = newStatus });
    }

    /// <summary>
    /// Logs a refund action.
    /// </summary>
    public Task<AuditLog> LogRefundAsync(
        int userId,
        int refundId,
        AuditActionType actionType,
        decimal amount,
        string? details = null,
        bool success = true,
        string? failureReason = null)
    {
        return LogActionAsync(
            userId,
            actionType,
            "Refund",
            refundId,
            $"Refund #{refundId}",
            success: success,
            failureReason: failureReason,
            details: details,
            newValue: new { Amount = amount });
    }

    /// <summary>
    /// Logs an account deletion.
    /// </summary>
    public Task<AuditLog> LogAccountDeletionAsync(
        int? initiatorUserId,
        int targetUserId,
        string targetEmail,
        string userType,
        string? reason = null)
    {
        return LogActionAsync(
            initiatorUserId,
            AuditActionType.AccountDeleted,
            "User",
            targetUserId,
            targetEmail,
            targetUserId: targetUserId,
            details: reason,
            newValue: new { UserType = userType, DeletedAt = DateTime.UtcNow });
    }

    /// <summary>
    /// Logs access to sensitive data.
    /// </summary>
    public Task<AuditLog> LogSensitiveDataAccessAsync(
        int userId,
        string entityType,
        int entityId,
        string? entityDisplayName = null,
        int? targetUserId = null)
    {
        return LogActionAsync(
            userId,
            AuditActionType.SensitiveDataAccessed,
            entityType,
            entityId,
            entityDisplayName,
            targetUserId: targetUserId);
    }
}
