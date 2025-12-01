using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Request data for bulk update operation.
/// </summary>
public class BulkUpdateRequest
{
    /// <summary>
    /// Gets or sets the list of product IDs to update.
    /// </summary>
    public required List<int> ProductIds { get; set; }

    /// <summary>
    /// Gets or sets the type of update (Price or Stock).
    /// </summary>
    public BulkUpdateType UpdateType { get; set; }

    /// <summary>
    /// Gets or sets the operation to perform.
    /// </summary>
    public BulkUpdateOperation Operation { get; set; }

    /// <summary>
    /// Gets or sets the value for the operation (fixed amount or percentage).
    /// </summary>
    public decimal Value { get; set; }
}

/// <summary>
/// Result of a bulk update operation.
/// </summary>
public class BulkUpdateResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful overall.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the number of products successfully updated.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the number of products that failed to update.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Gets or sets the list of errors for products that failed to update.
    /// </summary>
    public List<ProductBulkUpdateError> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the general error messages.
    /// </summary>
    public List<string> GeneralErrors { get; set; } = new();
}

/// <summary>
/// Preview of a product that will be updated.
/// </summary>
public class BulkUpdatePreviewItem
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product title.
    /// </summary>
    public string ProductTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    public decimal CurrentValue { get; set; }

    /// <summary>
    /// Gets or sets the new value after update.
    /// </summary>
    public decimal NewValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this update is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the error message if the update is invalid.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Interface for bulk product update service.
/// </summary>
public interface IBulkProductUpdateService
{
    /// <summary>
    /// Previews the impact of a bulk update operation.
    /// </summary>
    /// <param name="storeId">The store ID for ownership verification.</param>
    /// <param name="request">The bulk update request.</param>
    Task<List<BulkUpdatePreviewItem>> PreviewBulkUpdateAsync(int storeId, BulkUpdateRequest request);

    /// <summary>
    /// Executes a bulk update operation.
    /// </summary>
    /// <param name="storeId">The store ID for ownership verification.</param>
    /// <param name="request">The bulk update request.</param>
    /// <param name="userId">The user ID performing the update.</param>
    Task<BulkUpdateResult> ExecuteBulkUpdateAsync(int storeId, BulkUpdateRequest request, int userId);
}

/// <summary>
/// Service for bulk updating products.
/// </summary>
public class BulkProductUpdateService : IBulkProductUpdateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BulkProductUpdateService> _logger;

    public BulkProductUpdateService(
        ApplicationDbContext context,
        ILogger<BulkProductUpdateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<BulkUpdatePreviewItem>> PreviewBulkUpdateAsync(int storeId, BulkUpdateRequest request)
    {
        var productIdsSet = request.ProductIds.ToHashSet();
        var products = await _context.Products
            .Where(p => productIdsSet.Contains(p.Id) && p.StoreId == storeId)
            .ToListAsync();

        var preview = new List<BulkUpdatePreviewItem>();

        foreach (var product in products)
        {
            var currentValue = request.UpdateType == BulkUpdateType.Price ? product.Price : product.Stock;
            var newValue = CalculateNewValue(currentValue, request.Operation, request.Value);
            var (isValid, errorMessage) = ValidateNewValue(newValue, request.UpdateType);

            preview.Add(new BulkUpdatePreviewItem
            {
                ProductId = product.Id,
                ProductTitle = product.Title,
                CurrentValue = currentValue,
                NewValue = newValue,
                IsValid = isValid,
                ErrorMessage = errorMessage
            });
        }

        return preview;
    }

    /// <inheritdoc />
    public async Task<BulkUpdateResult> ExecuteBulkUpdateAsync(int storeId, BulkUpdateRequest request, int userId)
    {
        var result = new BulkUpdateResult();

        // Validate request
        if (request.ProductIds.Count == 0)
        {
            result.GeneralErrors.Add("No products selected for bulk update.");
            return result;
        }

        if (request.Value < 0 && request.Operation == BulkUpdateOperation.SetValue)
        {
            result.GeneralErrors.Add("Value cannot be negative when setting to a fixed value.");
            return result;
        }

        if (request.Operation == BulkUpdateOperation.IncreaseByPercent || 
            request.Operation == BulkUpdateOperation.DecreaseByPercent)
        {
            if (request.Value < 0)
            {
                result.GeneralErrors.Add("Percentage cannot be negative.");
                return result;
            }
        }

        // Get all products belonging to this store
        var productIdsSet = request.ProductIds.ToHashSet();
        var products = await _context.Products
            .Where(p => productIdsSet.Contains(p.Id) && p.StoreId == storeId && p.Status != ProductStatus.Archived)
            .ToListAsync();

        if (products.Count == 0)
        {
            result.GeneralErrors.Add("No valid products found for update.");
            return result;
        }

        var updateType = request.UpdateType == BulkUpdateType.Price ? "Price" : "Stock";
        var successfulUpdates = new List<string>();

        foreach (var product in products)
        {
            var currentValue = request.UpdateType == BulkUpdateType.Price ? product.Price : product.Stock;
            var newValue = CalculateNewValue(currentValue, request.Operation, request.Value);
            var (isValid, errorMessage) = ValidateNewValue(newValue, request.UpdateType);

            if (!isValid)
            {
                result.Errors.Add(new ProductBulkUpdateError
                {
                    ProductId = product.Id,
                    ProductTitle = product.Title,
                    ErrorMessage = errorMessage ?? "Invalid value",
                    CurrentValue = currentValue,
                    AttemptedValue = newValue
                });
                result.FailureCount++;
                continue;
            }

            // Update the product
            if (request.UpdateType == BulkUpdateType.Price)
            {
                product.Price = newValue;
                successfulUpdates.Add($"Product '{product.Title}' (ID: {product.Id}): Price {currentValue:C} -> {newValue:C}");
            }
            else
            {
                product.Stock = (int)newValue;
                successfulUpdates.Add($"Product '{product.Title}' (ID: {product.Id}): Stock {(int)currentValue} -> {(int)newValue}");
            }

            product.UpdatedAt = DateTime.UtcNow;
            result.SuccessCount++;
        }

        // Save all changes
        if (result.SuccessCount > 0)
        {
            await _context.SaveChangesAsync();

            // Log the bulk update operation
            _logger.LogInformation(
                "Bulk {UpdateType} update performed by user {UserId} on store {StoreId}. " +
                "Operation: {Operation}, Value: {Value}, Success: {SuccessCount}, Failures: {FailureCount}. " +
                "Updates: {Updates}",
                updateType,
                userId,
                storeId,
                request.Operation,
                request.Value,
                result.SuccessCount,
                result.FailureCount,
                string.Join("; ", successfulUpdates));
        }

        result.Success = result.SuccessCount > 0;
        return result;
    }

    /// <summary>
    /// Calculates the new value based on the operation and value.
    /// </summary>
    private static decimal CalculateNewValue(decimal currentValue, BulkUpdateOperation operation, decimal value)
    {
        return operation switch
        {
            BulkUpdateOperation.SetValue => value,
            BulkUpdateOperation.IncreaseBy => currentValue + value,
            BulkUpdateOperation.DecreaseBy => currentValue - value,
            BulkUpdateOperation.IncreaseByPercent => currentValue * (1 + value / 100m),
            BulkUpdateOperation.DecreaseByPercent => currentValue * (1 - value / 100m),
            _ => currentValue
        };
    }

    /// <summary>
    /// Validates the new value based on the update type.
    /// </summary>
    /// <returns>A tuple indicating if the value is valid and an error message if not.</returns>
    private static (bool IsValid, string? ErrorMessage) ValidateNewValue(decimal newValue, BulkUpdateType updateType)
    {
        if (updateType == BulkUpdateType.Price)
        {
            if (newValue <= 0)
            {
                return (false, "Price must be greater than zero.");
            }
            if (newValue > ProductService.MaxPrice)
            {
                return (false, $"Price must be less than {ProductService.MaxPrice + 0.01m:N0}.");
            }
        }
        else // Stock
        {
            if (newValue < 0)
            {
                return (false, "Stock cannot be negative.");
            }
            if (newValue > int.MaxValue)
            {
                return (false, "Stock value is too large.");
            }
        }

        return (true, null);
    }
}
