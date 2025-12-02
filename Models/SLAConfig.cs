using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents SLA (Service Level Agreement) configuration for case handling.
/// </summary>
public class SLAConfig
{
    /// <summary>
    /// Gets or sets the unique identifier for the SLA configuration.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the category this SLA applies to (null for default/global config).
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the category (navigation property).
    /// </summary>
    public Category? Category { get; set; }

    /// <summary>
    /// Gets or sets the request type this SLA applies to (null for both types).
    /// </summary>
    public ReturnRequestType? RequestType { get; set; }

    /// <summary>
    /// Gets or sets the first response deadline in hours (seller must respond within this time).
    /// </summary>
    public int FirstResponseHours { get; set; }

    /// <summary>
    /// Gets or sets the resolution deadline in hours (case must be resolved within this time).
    /// </summary>
    public int ResolutionHours { get; set; }

    /// <summary>
    /// Gets or sets whether this configuration is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when this configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when this configuration was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user ID who created/updated this configuration.
    /// </summary>
    public int? UpdatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user who created/updated this configuration (navigation property).
    /// </summary>
    public User? UpdatedByUser { get; set; }
}
