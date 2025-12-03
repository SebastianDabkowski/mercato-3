using MercatoApp.Data;
using MercatoApp.Helpers;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Result of a category operation.
/// </summary>
public class CategoryResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public Category? Category { get; set; }
}

/// <summary>
/// Data for creating a new category.
/// </summary>
public class CreateCategoryData
{
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public int? ParentCategoryId { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Data for updating an existing category.
/// </summary>
public class UpdateCategoryData
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public int? ParentCategoryId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Represents a category with its hierarchical path.
/// </summary>
public class CategoryTreeItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets the display name (last segment of the full path).
    /// </summary>
    public string DisplayName => Name;
    
    public int? ParentCategoryId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public int Level { get; set; }
    public int ProductCount { get; set; }
    public List<CategoryTreeItem> Children { get; set; } = new();
}

/// <summary>
/// Interface for category service.
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Creates a new category.
    /// </summary>
    Task<CategoryResult> CreateCategoryAsync(CreateCategoryData data);

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    Task<CategoryResult> UpdateCategoryAsync(int categoryId, UpdateCategoryData data);

    /// <summary>
    /// Deletes a category if it has no assigned products.
    /// </summary>
    Task<CategoryResult> DeleteCategoryAsync(int categoryId);

    /// <summary>
    /// Gets a category by its ID.
    /// </summary>
    Task<Category?> GetCategoryByIdAsync(int categoryId);

    /// <summary>
    /// Gets all categories as a flat list.
    /// </summary>
    Task<List<Category>> GetAllCategoriesAsync();

    /// <summary>
    /// Gets all active categories for assignment to products.
    /// </summary>
    Task<List<CategoryTreeItem>> GetActiveCategoriesForSelectionAsync();

    /// <summary>
    /// Gets all categories as a hierarchical tree with product counts.
    /// </summary>
    Task<List<CategoryTreeItem>> GetCategoryTreeAsync();

    /// <summary>
    /// Gets the count of products assigned to a category.
    /// </summary>
    Task<int> GetProductCountForCategoryAsync(int categoryId);

    /// <summary>
    /// Checks if a category has any child categories.
    /// </summary>
    Task<bool> HasChildCategoriesAsync(int categoryId);

    /// <summary>
    /// Validates that creating/moving a category to the specified parent wouldn't create a circular reference.
    /// </summary>
    Task<bool> WouldCreateCircularReferenceAsync(int categoryId, int? newParentId);

    /// <summary>
    /// Gets all descendant category IDs for a given category.
    /// </summary>
    /// <param name="categoryId">The parent category ID.</param>
    /// <returns>A set of all descendant category IDs.</returns>
    Task<HashSet<int>> GetDescendantCategoryIdsAsync(int categoryId);
}

/// <summary>
/// Service for managing product categories.
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CategoryService> _logger;

    /// <summary>
    /// Maximum length for category name.
    /// </summary>
    public const int MaxNameLength = 100;

    /// <summary>
    /// Maximum length for category slug.
    /// </summary>
    public const int MaxSlugLength = 150;

    /// <summary>
    /// Maximum length for category description.
    /// </summary>
    public const int MaxDescriptionLength = 500;

    public CategoryService(
        ApplicationDbContext context,
        ILogger<CategoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CategoryResult> CreateCategoryAsync(CreateCategoryData data)
    {
        var result = new CategoryResult();

        // Validate name
        if (string.IsNullOrWhiteSpace(data.Name))
        {
            result.Errors.Add("Category name is required.");
            return result;
        }

        var trimmedName = data.Name.Trim();
        if (trimmedName.Length > MaxNameLength)
        {
            result.Errors.Add($"Category name must be {MaxNameLength} characters or less.");
            return result;
        }

        // Generate or validate slug
        var slug = SlugGenerator.GenerateSlug(string.IsNullOrWhiteSpace(data.Slug) ? trimmedName : data.Slug);

        if (string.IsNullOrWhiteSpace(slug))
        {
            result.Errors.Add("Could not generate a valid slug. Please provide a different name or custom slug.");
            return result;
        }

        // Check for duplicate slug
        var slugExists = await _context.Categories.AnyAsync(c => c.Slug == slug);
        if (slugExists)
        {
            result.Errors.Add("A category with this slug already exists. Please use a different name or custom slug.");
            return result;
        }

        // Validate parent category exists if specified
        if (data.ParentCategoryId.HasValue)
        {
            var parentExists = await _context.Categories.AnyAsync(c => c.Id == data.ParentCategoryId.Value);
            if (!parentExists)
            {
                result.Errors.Add("Parent category not found.");
                return result;
            }
        }

        // Check for duplicate name at the same level
        var duplicateExists = await _context.Categories
            .AnyAsync(c => c.Name == trimmedName && c.ParentCategoryId == data.ParentCategoryId);
        if (duplicateExists)
        {
            result.Errors.Add("A category with this name already exists at this level.");
            return result;
        }

        var category = new Category
        {
            Name = trimmedName,
            Slug = slug,
            Description = data.Description?.Trim(),
            ParentCategoryId = data.ParentCategoryId,
            DisplayOrder = data.DisplayOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created category {CategoryId} with name '{CategoryName}' and slug '{Slug}'", category.Id, category.Name, category.Slug);

        result.Success = true;
        result.Category = category;
        return result;
    }

    /// <inheritdoc />
    public async Task<CategoryResult> UpdateCategoryAsync(int categoryId, UpdateCategoryData data)
    {
        var result = new CategoryResult();

        var category = await _context.Categories.FindAsync(categoryId);
        if (category == null)
        {
            result.Errors.Add("Category not found.");
            return result;
        }

        // Validate name
        if (string.IsNullOrWhiteSpace(data.Name))
        {
            result.Errors.Add("Category name is required.");
            return result;
        }

        var trimmedName = data.Name.Trim();
        if (trimmedName.Length > MaxNameLength)
        {
            result.Errors.Add($"Category name must be {MaxNameLength} characters or less.");
            return result;
        }

        // Validate and normalize slug
        if (string.IsNullOrWhiteSpace(data.Slug))
        {
            result.Errors.Add("Category slug is required.");
            return result;
        }

        var slug = SlugGenerator.GenerateSlug(data.Slug);
        if (string.IsNullOrWhiteSpace(slug))
        {
            result.Errors.Add("Could not generate a valid slug. Please provide a different slug.");
            return result;
        }

        // Check for duplicate slug (excluding current category)
        var slugExists = await _context.Categories.AnyAsync(c => c.Slug == slug && c.Id != categoryId);
        if (slugExists)
        {
            result.Errors.Add("A category with this slug already exists. Please use a different slug.");
            return result;
        }

        // Validate parent category exists if specified
        if (data.ParentCategoryId.HasValue)
        {
            // Cannot set self as parent
            if (data.ParentCategoryId.Value == categoryId)
            {
                result.Errors.Add("A category cannot be its own parent.");
                return result;
            }

            var parentExists = await _context.Categories.AnyAsync(c => c.Id == data.ParentCategoryId.Value);
            if (!parentExists)
            {
                result.Errors.Add("Parent category not found.");
                return result;
            }

            // Check for circular reference
            if (await WouldCreateCircularReferenceAsync(categoryId, data.ParentCategoryId.Value))
            {
                result.Errors.Add("Cannot set parent: this would create a circular reference.");
                return result;
            }
        }

        // Check for duplicate name at the same level (excluding current category)
        var duplicateExists = await _context.Categories
            .AnyAsync(c => c.Name == trimmedName && c.ParentCategoryId == data.ParentCategoryId && c.Id != categoryId);
        if (duplicateExists)
        {
            result.Errors.Add("A category with this name already exists at this level.");
            return result;
        }

        var oldName = category.Name;
        var oldSlug = category.Slug;
        var oldParentId = category.ParentCategoryId;
        var oldIsActive = category.IsActive;

        category.Name = trimmedName;
        category.Slug = slug;
        category.Description = data.Description?.Trim();
        category.ParentCategoryId = data.ParentCategoryId;
        category.DisplayOrder = data.DisplayOrder;
        category.IsActive = data.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Update the Category string on products if category name changed
        if (oldName != trimmedName)
        {
            var productsToUpdate = await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();
            
            foreach (var product in productsToUpdate)
            {
                product.Category = trimmedName;
                product.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation(
            "Updated category {CategoryId}: Name '{OldName}' -> '{NewName}', Slug '{OldSlug}' -> '{NewSlug}', ParentId {OldParentId} -> {NewParentId}, IsActive {OldIsActive} -> {NewIsActive}",
            categoryId, oldName, trimmedName, oldSlug, slug, oldParentId, data.ParentCategoryId, oldIsActive, data.IsActive);

        result.Success = true;
        result.Category = category;
        return result;
    }

    /// <inheritdoc />
    public async Task<CategoryResult> DeleteCategoryAsync(int categoryId)
    {
        var result = new CategoryResult();

        var category = await _context.Categories.FindAsync(categoryId);
        if (category == null)
        {
            result.Errors.Add("Category not found.");
            return result;
        }

        // Check for child categories
        var hasChildren = await HasChildCategoriesAsync(categoryId);
        if (hasChildren)
        {
            result.Errors.Add("Cannot delete category with child categories. Please move or delete child categories first.");
            return result;
        }

        // Check for assigned products
        var productCount = await GetProductCountForCategoryAsync(categoryId);
        if (productCount > 0)
        {
            result.Errors.Add($"Cannot delete category with {productCount} assigned product(s). Please reassign products to another category first.");
            return result;
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted category {CategoryId} with name '{CategoryName}'", categoryId, category.Name);

        result.Success = true;
        result.Category = category;
        return result;
    }

    /// <inheritdoc />
    public async Task<Category?> GetCategoryByIdAsync(int categoryId)
    {
        return await _context.Categories
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(c => c.Id == categoryId);
    }

    /// <inheritdoc />
    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        return await _context.Categories
            .Include(c => c.ParentCategory)
            .OrderBy(c => c.ParentCategoryId)
            .ThenBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<CategoryTreeItem>> GetActiveCategoriesForSelectionAsync()
    {
        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return BuildFlatTreeWithPaths(categories);
    }

    /// <inheritdoc />
    public async Task<List<CategoryTreeItem>> GetCategoryTreeAsync()
    {
        var categories = await _context.Categories
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        var productCounts = await _context.Products
            .Where(p => p.CategoryId.HasValue)
            .GroupBy(p => p.CategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

        return BuildHierarchicalTree(categories, productCounts);
    }

    /// <inheritdoc />
    public async Task<int> GetProductCountForCategoryAsync(int categoryId)
    {
        return await _context.Products.CountAsync(p => p.CategoryId == categoryId);
    }

    /// <inheritdoc />
    public async Task<bool> HasChildCategoriesAsync(int categoryId)
    {
        return await _context.Categories.AnyAsync(c => c.ParentCategoryId == categoryId);
    }

    /// <inheritdoc />
    public async Task<bool> WouldCreateCircularReferenceAsync(int categoryId, int? newParentId)
    {
        if (!newParentId.HasValue)
        {
            return false;
        }

        // Check if the new parent is a descendant of the category being moved
        var descendantIds = await GetDescendantCategoryIdsAsync(categoryId);
        return descendantIds.Contains(newParentId.Value);
    }

    /// <inheritdoc />
    public async Task<HashSet<int>> GetDescendantCategoryIdsAsync(int categoryId)
    {
        // Load all categories once to avoid N+1 queries
        var allCategories = await _context.Categories
            .Select(c => new { c.Id, c.ParentCategoryId })
            .ToListAsync();

        var descendantIds = new HashSet<int>();
        CollectDescendantsInMemory(categoryId, allCategories.ToDictionary(c => c.Id, c => c.ParentCategoryId), descendantIds);
        return descendantIds;
    }

    private static void CollectDescendantsInMemory(
        int categoryId,
        Dictionary<int, int?> categoryParentMap,
        HashSet<int> descendantIds)
    {
        var childIds = categoryParentMap.Where(kvp => kvp.Value == categoryId).Select(kvp => kvp.Key).ToList();
        foreach (var childId in childIds)
        {
            descendantIds.Add(childId);
            CollectDescendantsInMemory(childId, categoryParentMap, descendantIds);
        }
    }

    private List<CategoryTreeItem> BuildFlatTreeWithPaths(List<Category> categories)
    {
        var result = new List<CategoryTreeItem>();
        var categoryLookup = categories.ToDictionary(c => c.Id);

        foreach (var category in categories.Where(c => !c.ParentCategoryId.HasValue))
        {
            AddCategoryWithPath(category, "", 0, result, categoryLookup, categories);
        }

        return result;
    }

    private void AddCategoryWithPath(
        Category category,
        string parentPath,
        int level,
        List<CategoryTreeItem> result,
        Dictionary<int, Category> categoryLookup,
        List<Category> allCategories)
    {
        var fullPath = string.IsNullOrEmpty(parentPath) ? category.Name : $"{parentPath} > {category.Name}";
        
        result.Add(new CategoryTreeItem
        {
            Id = category.Id,
            Name = category.Name,
            FullPath = fullPath,
            ParentCategoryId = category.ParentCategoryId,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
            Level = level
        });

        var children = allCategories
            .Where(c => c.ParentCategoryId == category.Id)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name);

        foreach (var child in children)
        {
            AddCategoryWithPath(child, fullPath, level + 1, result, categoryLookup, allCategories);
        }
    }

    private List<CategoryTreeItem> BuildHierarchicalTree(
        List<Category> categories,
        Dictionary<int, int> productCounts)
    {
        var result = new List<CategoryTreeItem>();
        var categoryLookup = categories.ToDictionary(c => c.Id);

        foreach (var category in categories.Where(c => !c.ParentCategoryId.HasValue))
        {
            result.Add(BuildTreeItem(category, "", 0, categoryLookup, categories, productCounts));
        }

        return result;
    }

    private CategoryTreeItem BuildTreeItem(
        Category category,
        string parentPath,
        int level,
        Dictionary<int, Category> categoryLookup,
        List<Category> allCategories,
        Dictionary<int, int> productCounts)
    {
        var fullPath = string.IsNullOrEmpty(parentPath) ? category.Name : $"{parentPath} > {category.Name}";
        
        var item = new CategoryTreeItem
        {
            Id = category.Id,
            Name = category.Name,
            FullPath = fullPath,
            ParentCategoryId = category.ParentCategoryId,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
            Level = level,
            ProductCount = productCounts.TryGetValue(category.Id, out var count) ? count : 0
        };

        var children = allCategories
            .Where(c => c.ParentCategoryId == category.Id)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name);

        foreach (var child in children)
        {
            item.Children.Add(BuildTreeItem(child, fullPath, level + 1, categoryLookup, allCategories, productCounts));
        }

        return item;
    }
}
