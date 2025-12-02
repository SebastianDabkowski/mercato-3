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
                var searchTerm = filter.SearchQuery.Trim().ToLower();
                query = query.Where(u =>
                    u.Email.ToLower().Contains(searchTerm) ||
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    u.LastName.ToLower().Contains(searchTerm) ||
                    u.Id.ToString() == searchTerm);
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

            // Apply pagination
            var users = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
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

            // Get last login dates for each user
            var userIds = users.Select(u => u.Id).ToList();
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
}
