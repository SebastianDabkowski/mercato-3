namespace MercatoApp.Models;

/// <summary>
/// Filter criteria for querying admin audit logs.
/// </summary>
public class AdminAuditLogFilter
{
    /// <summary>
    /// Gets or sets the start date for filtering logs.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for filtering logs.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the admin user ID to filter by.
    /// </summary>
    public int? AdminUserId { get; set; }

    /// <summary>
    /// Gets or sets the entity type to filter by (e.g., "User", "Product", "Review").
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Gets or sets the entity ID to filter by.
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the action type to filter by.
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Gets or sets the page number for pagination.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size for pagination.
    /// </summary>
    public int PageSize { get; set; } = 50;
}
