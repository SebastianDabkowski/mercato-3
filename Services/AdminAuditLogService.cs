using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing admin audit logs.
/// </summary>
public class AdminAuditLogService : IAdminAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminAuditLogService> _logger;

    public AdminAuditLogService(
        ApplicationDbContext context,
        ILogger<AdminAuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PaginatedList<AdminAuditLog>> GetAuditLogsAsync(AdminAuditLogFilter filter)
    {
        var query = _context.AdminAuditLogs
            .Include(a => a.AdminUser)
            .Include(a => a.TargetUser)
            .AsQueryable();

        // Apply filters
        if (filter.StartDate.HasValue)
        {
            query = query.Where(a => a.ActionTimestamp >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(a => a.ActionTimestamp <= filter.EndDate.Value);
        }

        if (filter.AdminUserId.HasValue)
        {
            query = query.Where(a => a.AdminUserId == filter.AdminUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
        {
            query = query.Where(a => a.EntityType == filter.EntityType);
        }

        if (filter.EntityId.HasValue)
        {
            query = query.Where(a => a.EntityId == filter.EntityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(a => a.Action == filter.Action);
        }

        // Order by timestamp descending (most recent first)
        query = query.OrderByDescending(a => a.ActionTimestamp);

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PaginatedList<AdminAuditLog>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };
    }

    /// <inheritdoc />
    public async Task<AdminAuditLog?> GetAuditLogByIdAsync(int id)
    {
        return await _context.AdminAuditLogs
            .Include(a => a.AdminUser)
            .Include(a => a.TargetUser)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    /// <inheritdoc />
    public async Task<List<AdminAuditLog>> GetEntityAuditLogsAsync(string entityType, int entityId, int limit = 50)
    {
        return await _context.AdminAuditLogs
            .Include(a => a.AdminUser)
            .Include(a => a.TargetUser)
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.ActionTimestamp)
            .Take(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<AdminAuditLog> LogActionAsync(
        int adminUserId,
        string action,
        string entityType,
        int? entityId,
        string? entityDisplayName,
        int? targetUserId = null,
        string? reason = null,
        string? metadata = null)
    {
        var auditLog = new AdminAuditLog
        {
            AdminUserId = adminUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            EntityDisplayName = entityDisplayName,
            TargetUserId = targetUserId,
            Reason = reason,
            Metadata = metadata,
            ActionTimestamp = DateTime.UtcNow
        };

        _context.AdminAuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Admin action logged: {Action} on {EntityType} {EntityId} by admin user {AdminUserId}",
            action,
            entityType,
            entityId,
            adminUserId);

        return auditLog;
    }

    /// <inheritdoc />
    public async Task<List<string>> GetEntityTypesAsync()
    {
        return await _context.AdminAuditLogs
            .Where(a => !string.IsNullOrEmpty(a.EntityType))
            .Select(a => a.EntityType)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<string>> GetActionsAsync()
    {
        return await _context.AdminAuditLogs
            .Where(a => !string.IsNullOrEmpty(a.Action))
            .Select(a => a.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();
    }
}
