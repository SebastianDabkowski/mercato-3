using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MercatoApp.Models;

/// <summary>
/// Represents the relationship between a user and a store with their assigned role.
/// Internal users can have one role per store.
/// </summary>
public class StoreUserRole
{
    /// <summary>
    /// Gets or sets the unique identifier for the store user role.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the store ID.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the store navigation property.
    /// </summary>
    [ForeignKey(nameof(StoreId))]
    public Store Store { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the user navigation property.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the role assigned to the user for this store.
    /// </summary>
    public StoreRole Role { get; set; }

    /// <summary>
    /// Gets or sets whether this user role assignment is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the role was assigned.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the role was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the ID of the user who assigned this role (for audit purposes).
    /// </summary>
    public int? AssignedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user who assigned this role (navigation property).
    /// </summary>
    [ForeignKey(nameof(AssignedByUserId))]
    public User? AssignedByUser { get; set; }
}
