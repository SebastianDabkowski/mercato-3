using System.Security.Cryptography;
using System.Text;
using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for logging and managing audit logs for critical actions.
/// Implements tamper-evident logging with integrity verification.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        ApplicationDbContext context,
        ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
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
        string? previousValue = null,
        string? newValue = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? correlationId = null)
    {
        var auditLog = new AuditLog
        {
            UserId = userId,
            ActionType = actionType,
            EntityType = entityType,
            EntityId = entityId,
            EntityDisplayName = entityDisplayName,
            TargetUserId = targetUserId,
            Success = success,
            FailureReason = failureReason,
            Details = details,
            PreviousValue = previousValue,
            NewValue = newValue,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            IsArchived = false
        };

        // Calculate hash for tamper detection
        auditLog.EntryHash = CalculateHash(auditLog);

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Audit log created: ActionType={ActionType}, EntityType={EntityType}, EntityId={EntityId}, UserId={UserId}, Success={Success}",
            actionType,
            entityType,
            entityId,
            userId,
            success);

        return auditLog;
    }

    /// <inheritdoc />
    public async Task<PaginatedList<AuditLog>> GetAuditLogsAsync(AuditLogFilter filter)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .Include(a => a.TargetUser)
            .AsQueryable();

        // Apply filters
        if (filter.StartDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= filter.EndDate.Value);
        }

        if (filter.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == filter.UserId.Value);
        }

        if (filter.TargetUserId.HasValue)
        {
            query = query.Where(a => a.TargetUserId == filter.TargetUserId.Value);
        }

        if (filter.ActionType.HasValue)
        {
            query = query.Where(a => a.ActionType == filter.ActionType.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
        {
            query = query.Where(a => a.EntityType == filter.EntityType);
        }

        if (filter.EntityId.HasValue)
        {
            query = query.Where(a => a.EntityId == filter.EntityId.Value);
        }

        if (filter.Success.HasValue)
        {
            query = query.Where(a => a.Success == filter.Success.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.CorrelationId))
        {
            query = query.Where(a => a.CorrelationId == filter.CorrelationId);
        }

        if (!filter.IncludeArchived)
        {
            query = query.Where(a => !a.IsArchived);
        }

        // Order by timestamp descending (most recent first)
        query = query.OrderByDescending(a => a.Timestamp);

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PaginatedList<AuditLog>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };
    }

    /// <inheritdoc />
    public async Task<AuditLog?> GetAuditLogByIdAsync(int id)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Include(a => a.TargetUser)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    /// <inheritdoc />
    public async Task<List<AuditLog>> GetEntityAuditLogsAsync(string entityType, int entityId, int limit = 50)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Include(a => a.TargetUser)
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<string>> GetEntityTypesAsync()
    {
        return await _context.AuditLogs
            .Where(a => !string.IsNullOrEmpty(a.EntityType))
            .Select(a => a.EntityType)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> ArchiveOldLogsAsync(int retentionDays)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        const int batchSize = 1000;
        var totalArchived = 0;

        // Process in batches to avoid memory issues
        while (true)
        {
            var batch = await _context.AuditLogs
                .Where(a => a.Timestamp < cutoffDate && !a.IsArchived)
                .Take(batchSize)
                .ToListAsync();

            if (batch.Count == 0)
            {
                break;
            }

            foreach (var log in batch)
            {
                log.IsArchived = true;
                log.ArchivedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            totalArchived += batch.Count;

            if (batch.Count < batchSize)
            {
                break;
            }
        }

        if (totalArchived > 0)
        {
            _logger.LogInformation(
                "Archived {Count} audit logs older than {RetentionDays} days",
                totalArchived,
                retentionDays);
        }

        return totalArchived;
    }

    /// <inheritdoc />
    public async Task<int> DeleteArchivedLogsAsync(int retentionDays)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        const int batchSize = 1000;
        var totalDeleted = 0;

        // Process in batches to avoid memory issues
        while (true)
        {
            var batch = await _context.AuditLogs
                .Where(a => a.IsArchived && a.ArchivedAt.HasValue && a.ArchivedAt.Value < cutoffDate)
                .Take(batchSize)
                .ToListAsync();

            if (batch.Count == 0)
            {
                break;
            }

            _context.AuditLogs.RemoveRange(batch);
            await _context.SaveChangesAsync();
            totalDeleted += batch.Count;

            if (batch.Count < batchSize)
            {
                break;
            }
        }

        if (totalDeleted > 0)
        {
            _logger.LogInformation(
                "Deleted {Count} archived audit logs older than {RetentionDays} days",
                totalDeleted,
                retentionDays);
        }

        return totalDeleted;
    }

    /// <inheritdoc />
    public async Task<(int checkedCount, int tamperedCount)> VerifyIntegrityAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= endDate.Value);
        }

        var logs = await query.ToListAsync();
        var checkedCount = 0;
        var tamperedCount = 0;

        foreach (var log in logs)
        {
            checkedCount++;
            var calculatedHash = CalculateHash(log);
            
            if (log.EntryHash != calculatedHash)
            {
                tamperedCount++;
                _logger.LogWarning(
                    "Tampered audit log detected: Id={Id}, ActionType={ActionType}, Timestamp={Timestamp}",
                    log.Id,
                    log.ActionType,
                    log.Timestamp);
            }
        }

        if (tamperedCount > 0)
        {
            _logger.LogError(
                "Integrity check found {Tampered} tampered entries out of {Checked} checked",
                tamperedCount,
                checkedCount);
        }
        else if (checkedCount > 0)
        {
            _logger.LogInformation(
                "Integrity check passed: {Checked} entries verified with no tampering detected",
                checkedCount);
        }

        return (checkedCount, tamperedCount);
    }

    /// <summary>
    /// Calculates a hash for an audit log entry for tamper detection.
    /// </summary>
    private string CalculateHash(AuditLog log)
    {
        // Concatenate key fields to create a hash
        var data = $"{log.UserId}|{log.ActionType}|{log.EntityType}|{log.EntityId}|" +
                   $"{log.TargetUserId}|{log.Success}|{log.Timestamp:O}|" +
                   $"{log.PreviousValue}|{log.NewValue}|{log.Details}";

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hashBytes);
    }
}
