namespace MercatoApp.Models;

/// <summary>
/// Represents filter criteria for user management queries.
/// </summary>
public class UserManagementFilter
{
    /// <summary>
    /// Gets or sets the search query to filter by email, name, or user ID.
    /// </summary>
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Gets or sets the role filter.
    /// </summary>
    public string? RoleFilter { get; set; }

    /// <summary>
    /// Gets or sets the account status filter.
    /// </summary>
    public AccountStatus? StatusFilter { get; set; }

    /// <summary>
    /// Gets or sets the page number for pagination (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the sort field.
    /// </summary>
    public string SortBy { get; set; } = "CreatedAt";

    /// <summary>
    /// Gets or sets the sort direction (asc or desc).
    /// </summary>
    public string SortDirection { get; set; } = "desc";
}
