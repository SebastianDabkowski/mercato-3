using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a role in the system for extensible role-based access control.
/// </summary>
public class Role
{
    /// <summary>
    /// Gets or sets the unique identifier for the role.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the role (e.g., "Buyer", "Seller", "Admin").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the role.
    /// </summary>
    [MaxLength(256)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the role is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the role was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for role permissions.
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    /// <summary>
    /// Standard role names for the marketplace.
    /// </summary>
    public static class RoleNames
    {
        public const string Buyer = "Buyer";
        public const string Seller = "Seller";
        public const string Admin = "Admin";
        public const string Support = "Support";
        public const string Compliance = "Compliance";
    }
}
