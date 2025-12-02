using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for admin user management service.
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Gets a paginated list of users based on filter criteria.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <returns>A paginated list of users.</returns>
    Task<PaginatedList<UserListItem>> GetUsersAsync(UserManagementFilter filter);

    /// <summary>
    /// Gets detailed information about a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The user details, or null if not found.</returns>
    Task<User?> GetUserDetailsAsync(int userId);

    /// <summary>
    /// Gets the login history for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="limit">The maximum number of login events to return.</param>
    /// <returns>A list of login events.</returns>
    Task<List<LoginEvent>> GetLoginHistoryAsync(int userId, int limit = 10);

    /// <summary>
    /// Gets the role name for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The role name.</returns>
    Task<string> GetUserRoleAsync(int userId);

    /// <summary>
    /// Blocks a user account.
    /// </summary>
    /// <param name="userId">The user ID to block.</param>
    /// <param name="adminUserId">The admin user ID performing the action.</param>
    /// <param name="reason">The reason for blocking.</param>
    /// <param name="notes">Additional notes about the blocking.</param>
    /// <returns>True if the user was successfully blocked, false otherwise.</returns>
    Task<bool> BlockUserAsync(int userId, int adminUserId, BlockReason reason, string? notes);

    /// <summary>
    /// Unblocks a user account.
    /// </summary>
    /// <param name="userId">The user ID to unblock.</param>
    /// <param name="adminUserId">The admin user ID performing the action.</param>
    /// <param name="notes">Additional notes about the unblocking.</param>
    /// <param name="requirePasswordReset">Whether to require the user to reset their password on next login.</param>
    /// <returns>True if the user was successfully unblocked, false otherwise.</returns>
    Task<bool> UnblockUserAsync(int userId, int adminUserId, string? notes, bool requirePasswordReset = false);

    /// <summary>
    /// Gets the audit log entries for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="limit">The maximum number of entries to return.</param>
    /// <returns>A list of audit log entries.</returns>
    Task<List<AdminAuditLog>> GetUserAuditLogAsync(int userId, int limit = 10);
}
