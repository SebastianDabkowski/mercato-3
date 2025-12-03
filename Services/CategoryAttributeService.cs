using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Result of a category attribute operation.
/// </summary>
public class CategoryAttributeResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public CategoryAttribute? CategoryAttribute { get; set; }
}

/// <summary>
/// Data for creating a new category attribute.
/// </summary>
public class CreateCategoryAttributeData
{
    public required int CategoryId { get; set; }
    public required string Name { get; set; }
    public string? DisplayLabel { get; set; }
    public string? Description { get; set; }
    public required AttributeType AttributeType { get; set; }
    public bool IsRequired { get; set; }
    public bool IsFilterable { get; set; }
    public bool IsSearchable { get; set; }
    public int DisplayOrder { get; set; }
    public string? ValidationPattern { get; set; }
    public string? ValidationMessage { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string? Unit { get; set; }
    public List<string>? Options { get; set; }
}

/// <summary>
/// Data for updating an existing category attribute.
/// </summary>
public class UpdateCategoryAttributeData
{
    public required string Name { get; set; }
    public string? DisplayLabel { get; set; }
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public bool IsFilterable { get; set; }
    public bool IsSearchable { get; set; }
    public int DisplayOrder { get; set; }
    public string? ValidationPattern { get; set; }
    public string? ValidationMessage { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string? Unit { get; set; }
    public List<string>? Options { get; set; }
}

/// <summary>
/// Interface for category attribute service.
/// </summary>
public interface ICategoryAttributeService
{
    /// <summary>
    /// Creates a new category attribute.
    /// </summary>
    Task<CategoryAttributeResult> CreateAttributeAsync(CreateCategoryAttributeData data);

    /// <summary>
    /// Updates an existing category attribute.
    /// </summary>
    Task<CategoryAttributeResult> UpdateAttributeAsync(int attributeId, UpdateCategoryAttributeData data);

    /// <summary>
    /// Marks an attribute as deprecated.
    /// Deprecated attributes are hidden from new product creation but remain visible for existing products.
    /// </summary>
    Task<CategoryAttributeResult> DeprecateAttributeAsync(int attributeId);

    /// <summary>
    /// Restores a deprecated attribute.
    /// </summary>
    Task<CategoryAttributeResult> RestoreAttributeAsync(int attributeId);

    /// <summary>
    /// Deletes an attribute if it has no associated product values.
    /// </summary>
    Task<CategoryAttributeResult> DeleteAttributeAsync(int attributeId);

    /// <summary>
    /// Gets a category attribute by its ID.
    /// </summary>
    Task<CategoryAttribute?> GetAttributeByIdAsync(int attributeId);

    /// <summary>
    /// Gets all attributes for a category.
    /// </summary>
    Task<List<CategoryAttribute>> GetAttributesForCategoryAsync(int categoryId, bool includeDeprecated = true);

    /// <summary>
    /// Gets active (non-deprecated) attributes for a category.
    /// </summary>
    Task<List<CategoryAttribute>> GetActiveAttributesForCategoryAsync(int categoryId);

    /// <summary>
    /// Checks if an attribute has any product values.
    /// </summary>
    Task<bool> HasProductValuesAsync(int attributeId);

    /// <summary>
    /// Gets the count of products using this attribute.
    /// </summary>
    Task<int> GetProductCountForAttributeAsync(int attributeId);

    /// <summary>
    /// Gets product counts for multiple attributes in a single query.
    /// </summary>
    /// <param name="attributeIds">The attribute IDs to get counts for.</param>
    /// <returns>A dictionary mapping attribute ID to product count.</returns>
    Task<Dictionary<int, int>> GetProductCountsForAttributesAsync(IEnumerable<int> attributeIds);
}

/// <summary>
/// Service for managing category attributes.
/// </summary>
public class CategoryAttributeService : ICategoryAttributeService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CategoryAttributeService> _logger;

    /// <summary>
    /// Maximum length for attribute name.
    /// </summary>
    public const int MaxNameLength = 100;

    /// <summary>
    /// Maximum length for attribute description.
    /// </summary>
    public const int MaxDescriptionLength = 500;

    public CategoryAttributeService(
        ApplicationDbContext context,
        ILogger<CategoryAttributeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CategoryAttributeResult> CreateAttributeAsync(CreateCategoryAttributeData data)
    {
        var result = new CategoryAttributeResult();

        // Validate category exists
        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == data.CategoryId);
        if (!categoryExists)
        {
            result.Errors.Add("Category not found.");
            return result;
        }

        // Validate name
        if (string.IsNullOrWhiteSpace(data.Name))
        {
            result.Errors.Add("Attribute name is required.");
            return result;
        }

        var trimmedName = data.Name.Trim();
        if (trimmedName.Length > MaxNameLength)
        {
            result.Errors.Add($"Attribute name must be {MaxNameLength} characters or less.");
            return result;
        }

        // Check for duplicate name within the category
        var duplicateExists = await _context.CategoryAttributes
            .AnyAsync(a => a.CategoryId == data.CategoryId && a.Name == trimmedName);
        if (duplicateExists)
        {
            result.Errors.Add("An attribute with this name already exists for this category.");
            return result;
        }

        // Validate select-type attributes have options
        if ((data.AttributeType == AttributeType.SingleSelect || data.AttributeType == AttributeType.MultiSelect) 
            && (data.Options == null || data.Options.Count == 0))
        {
            result.Errors.Add("Select-type attributes must have at least one option.");
            return result;
        }

        // Validate min/max values for number-type attributes
        if (data.AttributeType == AttributeType.Number && data.MinValue.HasValue && data.MaxValue.HasValue)
        {
            if (data.MinValue.Value > data.MaxValue.Value)
            {
                result.Errors.Add("Minimum value cannot be greater than maximum value.");
                return result;
            }
        }

        var attribute = new CategoryAttribute
        {
            CategoryId = data.CategoryId,
            Name = trimmedName,
            DisplayLabel = string.IsNullOrWhiteSpace(data.DisplayLabel) ? null : data.DisplayLabel.Trim(),
            Description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim(),
            AttributeType = data.AttributeType,
            IsRequired = data.IsRequired,
            IsFilterable = data.IsFilterable,
            IsSearchable = data.IsSearchable,
            DisplayOrder = data.DisplayOrder,
            ValidationPattern = string.IsNullOrWhiteSpace(data.ValidationPattern) ? null : data.ValidationPattern.Trim(),
            ValidationMessage = string.IsNullOrWhiteSpace(data.ValidationMessage) ? null : data.ValidationMessage.Trim(),
            MinValue = data.MinValue,
            MaxValue = data.MaxValue,
            Unit = string.IsNullOrWhiteSpace(data.Unit) ? null : data.Unit.Trim(),
            IsDeprecated = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CategoryAttributes.Add(attribute);
        await _context.SaveChangesAsync();

        // Add options if select-type attribute
        if (data.Options != null && data.Options.Count > 0)
        {
            var displayOrder = 0;
            foreach (var optionValue in data.Options)
            {
                if (!string.IsNullOrWhiteSpace(optionValue))
                {
                    var option = new CategoryAttributeOption
                    {
                        CategoryAttributeId = attribute.Id,
                        Value = optionValue.Trim(),
                        DisplayOrder = displayOrder++,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.CategoryAttributeOptions.Add(option);
                }
            }
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation(
            "Created category attribute {AttributeId} with name '{AttributeName}' for category {CategoryId}",
            attribute.Id, attribute.Name, data.CategoryId);

        result.Success = true;
        result.CategoryAttribute = attribute;
        return result;
    }

    /// <inheritdoc />
    public async Task<CategoryAttributeResult> UpdateAttributeAsync(int attributeId, UpdateCategoryAttributeData data)
    {
        var result = new CategoryAttributeResult();

        var attribute = await _context.CategoryAttributes
            .Include(a => a.Options)
            .FirstOrDefaultAsync(a => a.Id == attributeId);

        if (attribute == null)
        {
            result.Errors.Add("Attribute not found.");
            return result;
        }

        // Validate name
        if (string.IsNullOrWhiteSpace(data.Name))
        {
            result.Errors.Add("Attribute name is required.");
            return result;
        }

        var trimmedName = data.Name.Trim();
        if (trimmedName.Length > MaxNameLength)
        {
            result.Errors.Add($"Attribute name must be {MaxNameLength} characters or less.");
            return result;
        }

        // Check for duplicate name within the category (excluding current attribute)
        var duplicateExists = await _context.CategoryAttributes
            .AnyAsync(a => a.CategoryId == attribute.CategoryId && a.Name == trimmedName && a.Id != attributeId);
        if (duplicateExists)
        {
            result.Errors.Add("An attribute with this name already exists for this category.");
            return result;
        }

        // Validate select-type attributes have options
        if ((attribute.AttributeType == AttributeType.SingleSelect || attribute.AttributeType == AttributeType.MultiSelect) 
            && (data.Options == null || data.Options.Count == 0))
        {
            result.Errors.Add("Select-type attributes must have at least one option.");
            return result;
        }

        // Validate min/max values for number-type attributes
        if (attribute.AttributeType == AttributeType.Number && data.MinValue.HasValue && data.MaxValue.HasValue)
        {
            if (data.MinValue.Value > data.MaxValue.Value)
            {
                result.Errors.Add("Minimum value cannot be greater than maximum value.");
                return result;
            }
        }

        attribute.Name = trimmedName;
        attribute.DisplayLabel = string.IsNullOrWhiteSpace(data.DisplayLabel) ? null : data.DisplayLabel.Trim();
        attribute.Description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim();
        attribute.IsRequired = data.IsRequired;
        attribute.IsFilterable = data.IsFilterable;
        attribute.IsSearchable = data.IsSearchable;
        attribute.DisplayOrder = data.DisplayOrder;
        attribute.ValidationPattern = string.IsNullOrWhiteSpace(data.ValidationPattern) ? null : data.ValidationPattern.Trim();
        attribute.ValidationMessage = string.IsNullOrWhiteSpace(data.ValidationMessage) ? null : data.ValidationMessage.Trim();
        attribute.MinValue = data.MinValue;
        attribute.MaxValue = data.MaxValue;
        attribute.Unit = string.IsNullOrWhiteSpace(data.Unit) ? null : data.Unit.Trim();
        attribute.UpdatedAt = DateTime.UtcNow;

        // Update options if select-type attribute
        if (data.Options != null && (attribute.AttributeType == AttributeType.SingleSelect || attribute.AttributeType == AttributeType.MultiSelect))
        {
            // Use differential update to preserve existing option IDs and avoid breaking product value references
            var newOptionValues = data.Options
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Select(o => o.Trim())
                .ToList();

            var existingOptions = attribute.Options.ToList();

            // Mark options that are no longer in the list as inactive instead of deleting
            foreach (var existingOption in existingOptions)
            {
                if (!newOptionValues.Contains(existingOption.Value))
                {
                    existingOption.IsActive = false;
                }
            }

            // Add new options that don't exist yet
            var displayOrder = 0;
            foreach (var optionValue in newOptionValues)
            {
                var existingOption = existingOptions.FirstOrDefault(o => o.Value == optionValue);
                if (existingOption != null)
                {
                    // Update existing option
                    existingOption.DisplayOrder = displayOrder++;
                    existingOption.IsActive = true;
                }
                else
                {
                    // Add new option
                    var option = new CategoryAttributeOption
                    {
                        CategoryAttributeId = attribute.Id,
                        Value = optionValue,
                        DisplayOrder = displayOrder++,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.CategoryAttributeOptions.Add(option);
                }
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated category attribute {AttributeId} with name '{AttributeName}'", attributeId, attribute.Name);

        result.Success = true;
        result.CategoryAttribute = attribute;
        return result;
    }

    /// <inheritdoc />
    public async Task<CategoryAttributeResult> DeprecateAttributeAsync(int attributeId)
    {
        var result = new CategoryAttributeResult();

        var attribute = await _context.CategoryAttributes.FindAsync(attributeId);
        if (attribute == null)
        {
            result.Errors.Add("Attribute not found.");
            return result;
        }

        if (attribute.IsDeprecated)
        {
            result.Errors.Add("Attribute is already deprecated.");
            return result;
        }

        attribute.IsDeprecated = true;
        attribute.DeprecatedAt = DateTime.UtcNow;
        attribute.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deprecated category attribute {AttributeId} with name '{AttributeName}'", attributeId, attribute.Name);

        result.Success = true;
        result.CategoryAttribute = attribute;
        return result;
    }

    /// <inheritdoc />
    public async Task<CategoryAttributeResult> RestoreAttributeAsync(int attributeId)
    {
        var result = new CategoryAttributeResult();

        var attribute = await _context.CategoryAttributes.FindAsync(attributeId);
        if (attribute == null)
        {
            result.Errors.Add("Attribute not found.");
            return result;
        }

        if (!attribute.IsDeprecated)
        {
            result.Errors.Add("Attribute is not deprecated.");
            return result;
        }

        attribute.IsDeprecated = false;
        attribute.DeprecatedAt = null;
        attribute.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Restored category attribute {AttributeId} with name '{AttributeName}'", attributeId, attribute.Name);

        result.Success = true;
        result.CategoryAttribute = attribute;
        return result;
    }

    /// <inheritdoc />
    public async Task<CategoryAttributeResult> DeleteAttributeAsync(int attributeId)
    {
        var result = new CategoryAttributeResult();

        var attribute = await _context.CategoryAttributes
            .Include(a => a.Options)
            .FirstOrDefaultAsync(a => a.Id == attributeId);

        if (attribute == null)
        {
            result.Errors.Add("Attribute not found.");
            return result;
        }

        // Check if attribute has product values
        var hasValues = await HasProductValuesAsync(attributeId);
        if (hasValues)
        {
            var productCount = await GetProductCountForAttributeAsync(attributeId);
            result.Errors.Add($"Cannot delete attribute because it has {productCount} associated product value(s). Consider deprecating it instead.");
            return result;
        }

        _context.CategoryAttributes.Remove(attribute);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted category attribute {AttributeId} with name '{AttributeName}'", attributeId, attribute.Name);

        result.Success = true;
        result.CategoryAttribute = attribute;
        return result;
    }

    /// <inheritdoc />
    public async Task<CategoryAttribute?> GetAttributeByIdAsync(int attributeId)
    {
        return await _context.CategoryAttributes
            .Include(a => a.Options.OrderBy(o => o.DisplayOrder))
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.Id == attributeId);
    }

    /// <inheritdoc />
    public async Task<List<CategoryAttribute>> GetAttributesForCategoryAsync(int categoryId, bool includeDeprecated = true)
    {
        var query = _context.CategoryAttributes
            .Include(a => a.Options.OrderBy(o => o.DisplayOrder))
            .Where(a => a.CategoryId == categoryId);

        if (!includeDeprecated)
        {
            query = query.Where(a => !a.IsDeprecated);
        }

        return await query
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<CategoryAttribute>> GetActiveAttributesForCategoryAsync(int categoryId)
    {
        return await GetAttributesForCategoryAsync(categoryId, includeDeprecated: false);
    }

    /// <inheritdoc />
    public async Task<bool> HasProductValuesAsync(int attributeId)
    {
        return await _context.ProductAttributeValues.AnyAsync(v => v.CategoryAttributeId == attributeId);
    }

    /// <inheritdoc />
    public async Task<int> GetProductCountForAttributeAsync(int attributeId)
    {
        return await _context.ProductAttributeValues
            .Where(v => v.CategoryAttributeId == attributeId)
            .Select(v => v.ProductId)
            .Distinct()
            .CountAsync();
    }

    /// <inheritdoc />
    public async Task<Dictionary<int, int>> GetProductCountsForAttributesAsync(IEnumerable<int> attributeIds)
    {
        var attributeIdList = attributeIds.ToList();
        if (attributeIdList.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        var counts = await _context.ProductAttributeValues
            .Where(v => attributeIdList.Contains(v.CategoryAttributeId))
            .GroupBy(v => v.CategoryAttributeId)
            .Select(g => new { AttributeId = g.Key, Count = g.Select(v => v.ProductId).Distinct().Count() })
            .ToDictionaryAsync(x => x.AttributeId, x => x.Count);

        // Ensure all requested attribute IDs are in the dictionary, even if count is 0
        foreach (var id in attributeIdList)
        {
            if (!counts.ContainsKey(id))
            {
                counts[id] = 0;
            }
        }

        return counts;
    }
}
