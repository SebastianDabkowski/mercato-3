using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a mapping between roles and permissions.
/// </summary>
public class RolePermission
{
    /// <summary>
    /// Gets or sets the unique identifier for the role-permission mapping.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the role ID.
    /// </summary>
    [Required]
    public int RoleId { get; set; }

    /// <summary>
    /// Gets or sets the permission ID.
    /// </summary>
    [Required]
    public int PermissionId { get; set; }

    /// <summary>
    /// Gets or sets whether this role-permission mapping is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when this mapping was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the ID of the user who granted this permission.
    /// </summary>
    public int? GrantedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this mapping was last modified.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who last modified this mapping.
    /// </summary>
    public int? ModifiedByUserId { get; set; }

    /// <summary>
    /// Navigation property for the role.
    /// </summary>
    public virtual Role Role { get; set; } = null!;

    /// <summary>
    /// Navigation property for the permission.
    /// </summary>
    public virtual Permission Permission { get; set; } = null!;
}
