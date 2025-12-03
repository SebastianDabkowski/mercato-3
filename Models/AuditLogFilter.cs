namespace MercatoApp.Models;

/// <summary>
/// Filter criteria for querying audit logs.
/// </summary>
public class AuditLogFilter
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
    /// Gets or sets the user ID who performed the action.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the target user ID (for user-related actions).
    /// </summary>
    public int? TargetUserId { get; set; }

    /// <summary>
    /// Gets or sets the action type to filter by.
    /// </summary>
    public AuditActionType? ActionType { get; set; }

    /// <summary>
    /// Gets or sets the entity type to filter by (e.g., "User", "Order", "Payout").
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Gets or sets the entity ID to filter by.
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>
    /// Gets or sets whether to filter by success/failure.
    /// </summary>
    public bool? Success { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for filtering related entries.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets whether to include archived entries.
    /// </summary>
    public bool IncludeArchived { get; set; }

    /// <summary>
    /// Gets or sets the page number for pagination.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size for pagination.
    /// </summary>
    public int PageSize { get; set; } = 50;
}
