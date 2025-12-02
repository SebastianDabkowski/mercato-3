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
}
