using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing users in the admin panel.
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        ApplicationDbContext context,
        ILogger<UserManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PaginatedList<UserListItem>> GetUsersAsync(UserManagementFilter filter)
    {
        try
        {
            // Start with all users query
            var query = _context.Users.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                var searchTerm = filter.SearchQuery.Trim();
                
                // Try parsing as integer for ID search
                if (int.TryParse(searchTerm, out var userId))
                {
                    query = query.Where(u =>
                        u.Id == userId ||
                        EF.Functions.Like(u.Email, $"%{searchTerm}%") ||
                        EF.Functions.Like(u.FirstName, $"%{searchTerm}%") ||
                        EF.Functions.Like(u.LastName, $"%{searchTerm}%"));
                }
                else
                {
                    query = query.Where(u =>
                        EF.Functions.Like(u.Email, $"%{searchTerm}%") ||
                        EF.Functions.Like(u.FirstName, $"%{searchTerm}%") ||
                        EF.Functions.Like(u.LastName, $"%{searchTerm}%"));
                }
            }

            // Apply status filter
            if (filter.StatusFilter.HasValue)
            {
                query = query.Where(u => u.Status == filter.StatusFilter.Value);
            }

            // Apply role filter (based on UserType)
            if (!string.IsNullOrWhiteSpace(filter.RoleFilter))
            {
                if (Enum.TryParse<UserType>(filter.RoleFilter, true, out var userType))
                {
                    query = query.Where(u => u.UserType == userType);
                }
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = filter.SortBy.ToLower() switch
            {
                "email" => filter.SortDirection.ToLower() == "asc"
                    ? query.OrderBy(u => u.Email)
                    : query.OrderByDescending(u => u.Email),
                "name" => filter.SortDirection.ToLower() == "asc"
                    ? query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
                    : query.OrderByDescending(u => u.LastName).ThenByDescending(u => u.FirstName),
                "status" => filter.SortDirection.ToLower() == "asc"
                    ? query.OrderBy(u => u.Status)
                    : query.OrderByDescending(u => u.Status),
                "role" => filter.SortDirection.ToLower() == "asc"
                    ? query.OrderBy(u => u.UserType)
                    : query.OrderByDescending(u => u.UserType),
                _ => filter.SortDirection.ToLower() == "asc"
                    ? query.OrderBy(u => u.CreatedAt)
                    : query.OrderByDescending(u => u.CreatedAt)
            };

            // Apply pagination and get users with last login in a single query
            var userIds = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(u => u.Id)
                .ToListAsync();

            // Get last login dates for the paginated users only
            var lastLogins = await _context.LoginEvents
                .Where(le => userIds.Contains(le.UserId ?? 0) && le.IsSuccessful)
                .GroupBy(le => le.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    LastLoginAt = g.Max(le => le.CreatedAt)
                })
                .ToListAsync();

            var lastLoginDict = lastLogins.ToDictionary(ll => ll.UserId ?? 0, ll => ll.LastLoginAt);

            // Get the actual user data
            var users = await query
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.UserType,
                    u.Status,
                    u.CreatedAt
                })
                .ToListAsync();

            // Map to UserListItem
            var items = users.Select(u => new UserListItem
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.UserType.ToString(),
                Status = u.Status,
                CreatedAt = u.CreatedAt,
                LastLoginAt = lastLoginDict.TryGetValue(u.Id, out var lastLogin) ? lastLogin : null
            }).ToList();

            return new PaginatedList<UserListItem>
            {
                Items = items,
                PageNumber = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users list with filter");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<User?> GetUserDetailsAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    /// <inheritdoc />
    public async Task<List<LoginEvent>> GetLoginHistoryAsync(int userId, int limit = 10)
    {
        return await _context.LoginEvents
            .Where(le => le.UserId == userId)
            .OrderByDescending(le => le.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<string> GetUserRoleAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.UserType.ToString() ?? "Unknown";
    }

    /// <inheritdoc />
    public async Task<bool> BlockUserAsync(int userId, int adminUserId, BlockReason reason, string? notes)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Attempted to block non-existent user {UserId}", userId);
                return false;
            }

            // Check if user is already blocked
            if (user.Status == AccountStatus.Blocked)
            {
                _logger.LogInformation("User {UserId} is already blocked", userId);
                return false;
            }

            var previousStatus = user.Status;

            // Update user status and blocking information
            user.Status = AccountStatus.Blocked;
            user.BlockedByUserId = adminUserId;
            user.BlockedAt = DateTime.UtcNow;
            user.BlockReason = reason;
            user.BlockNotes = notes;

            // Create audit log entry
            var auditLog = new AdminAuditLog
            {
                AdminUserId = adminUserId,
                TargetUserId = userId,
                Action = "BlockUser",
                Reason = $"{reason}: {notes}",
                ActionTimestamp = DateTime.UtcNow,
                Metadata = $"Previous status: {previousStatus}, Reason: {reason}"
            };

            _context.AdminAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} blocked by admin {AdminUserId} for reason {Reason}", 
                userId, adminUserId, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blocking user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UnblockUserAsync(int userId, int adminUserId, string? notes)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Attempted to unblock non-existent user {UserId}", userId);
                return false;
            }

            // Check if user is actually blocked
            if (user.Status != AccountStatus.Blocked)
            {
                _logger.LogInformation("User {UserId} is not currently blocked", userId);
                return false;
            }

            // Update user status - set to Active (they were previously active before being blocked)
            user.Status = AccountStatus.Active;
            
            // Keep block history for audit purposes, don't clear BlockedByUserId, BlockedAt, etc.

            // Create audit log entry
            var auditLog = new AdminAuditLog
            {
                AdminUserId = adminUserId,
                TargetUserId = userId,
                Action = "UnblockUser",
                Reason = notes,
                ActionTimestamp = DateTime.UtcNow,
                Metadata = $"Previous status: Blocked, New status: Active"
            };

            _context.AdminAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} unblocked by admin {AdminUserId}", userId, adminUserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unblocking user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<AdminAuditLog>> GetUserAuditLogAsync(int userId, int limit = 10)
    {
        try
        {
            return await _context.AdminAuditLogs
                .Where(log => log.TargetUserId == userId)
                .Include(log => log.AdminUser)
                .OrderByDescending(log => log.ActionTimestamp)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit log for user {UserId}", userId);
            throw;
        }
    }
}
