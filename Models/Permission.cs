using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a permission in the system for granular access control.
/// </summary>
public class Permission
{
    /// <summary>
    /// Gets or sets the unique identifier for the permission.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the permission (e.g., "ViewProducts", "ManageUsers").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the module or area this permission belongs to (e.g., "Products", "Users", "Orders").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the permission.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the permission is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the permission was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for role permissions.
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    /// <summary>
    /// Standard permission names organized by module.
    /// </summary>
    public static class PermissionNames
    {
        // Product permissions
        public const string ViewProducts = "ViewProducts";
        public const string CreateProducts = "CreateProducts";
        public const string EditProducts = "EditProducts";
        public const string DeleteProducts = "DeleteProducts";
        public const string ModerateProducts = "ModerateProducts";

        // Order permissions
        public const string ViewOrders = "ViewOrders";
        public const string ManageOrders = "ManageOrders";
        public const string ViewAllOrders = "ViewAllOrders";

        // User permissions
        public const string ViewUsers = "ViewUsers";
        public const string ManageUsers = "ManageUsers";
        public const string BlockUsers = "BlockUsers";

        // Store permissions
        public const string ViewStores = "ViewStores";
        public const string ManageOwnStore = "ManageOwnStore";
        public const string ManageAllStores = "ManageAllStores";

        // Review permissions
        public const string ViewReviews = "ViewReviews";
        public const string WriteReviews = "WriteReviews";
        public const string ModerateReviews = "ModerateReviews";

        // Support permissions
        public const string ViewSupportTickets = "ViewSupportTickets";
        public const string ManageSupportTickets = "ManageSupportTickets";

        // Compliance permissions
        public const string ViewComplianceReports = "ViewComplianceReports";
        public const string ManageCompliance = "ManageCompliance";
        public const string AccessAuditLogs = "AccessAuditLogs";

        // Cart permissions
        public const string ManageCart = "ManageCart";

        // Dashboard permissions
        public const string ViewBuyerDashboard = "ViewBuyerDashboard";
        public const string ViewSellerDashboard = "ViewSellerDashboard";
        public const string ViewAdminDashboard = "ViewAdminDashboard";

        // Configuration permissions
        public const string ManageRoles = "ManageRoles";
        public const string ManagePermissions = "ManagePermissions";
        public const string ManageSettings = "ManageSettings";
    }

    /// <summary>
    /// Standard module names.
    /// </summary>
    public static class ModuleNames
    {
        public const string Products = "Products";
        public const string Orders = "Orders";
        public const string Users = "Users";
        public const string Stores = "Stores";
        public const string Reviews = "Reviews";
        public const string Support = "Support";
        public const string Compliance = "Compliance";
        public const string Cart = "Cart";
        public const string Dashboard = "Dashboard";
        public const string Configuration = "Configuration";
    }
}
