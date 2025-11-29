using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a product category in the marketplace.
/// Supports hierarchical structure with parent-child relationships.
/// </summary>
public class Category
{
    /// <summary>
    /// Gets or sets the unique identifier for the category.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent category ID for hierarchical structure.
    /// Null indicates a root-level category.
    /// </summary>
    public int? ParentCategoryId { get; set; }

    /// <summary>
    /// Gets or sets the parent category (navigation property).
    /// </summary>
    public Category? ParentCategory { get; set; }

    /// <summary>
    /// Gets or sets the child categories (navigation property).
    /// </summary>
    public ICollection<Category> ChildCategories { get; set; } = new List<Category>();

    /// <summary>
    /// Gets or sets the display order for sorting categories.
    /// Lower values appear first.
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether the category is active.
    /// Inactive categories are hidden from sellers when assigning products.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the category was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the category was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
