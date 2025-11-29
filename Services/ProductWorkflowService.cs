using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Result of a product workflow state transition.
/// </summary>
public class WorkflowTransitionResult
{
    /// <summary>
    /// Indicates whether the workflow transition was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of errors that occurred during the transition.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of fields that must be fixed before the transition can be completed.
    /// </summary>
    public List<string> InvalidFields { get; set; } = new();

    /// <summary>
    /// The product after the transition (if successful).
    /// </summary>
    public Product? Product { get; set; }
}

/// <summary>
/// Interface for product workflow service.
/// Manages product workflow state transitions with validation.
/// </summary>
public interface IProductWorkflowService
{
    /// <summary>
    /// Validates whether a product meets the minimum data quality requirements to be activated.
    /// Active products must have: title, description, category, price > 0, stock >= 0, and at least one image.
    /// </summary>
    /// <param name="product">The product to validate.</param>
    /// <returns>A result containing validation errors if any.</returns>
    WorkflowTransitionResult ValidateForActivation(Product product);

    /// <summary>
    /// Checks if a workflow transition is allowed based on business rules.
    /// </summary>
    /// <param name="currentStatus">The current product status.</param>
    /// <param name="newStatus">The target product status.</param>
    /// <param name="isAdmin">Whether the user is an admin (admins can override some restrictions).</param>
    /// <returns>True if the transition is allowed; otherwise false.</returns>
    bool IsTransitionAllowed(ProductStatus currentStatus, ProductStatus newStatus, bool isAdmin = false);

    /// <summary>
    /// Gets the list of allowed status transitions from the current status.
    /// </summary>
    /// <param name="currentStatus">The current product status.</param>
    /// <param name="isAdmin">Whether the user is an admin.</param>
    /// <returns>List of allowed target statuses.</returns>
    IEnumerable<ProductStatus> GetAllowedTransitions(ProductStatus currentStatus, bool isAdmin = false);

    /// <summary>
    /// Attempts to change the workflow state of a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="storeId">The store ID for ownership verification (null for admin override).</param>
    /// <param name="newStatus">The target status.</param>
    /// <param name="userId">The user ID performing the change.</param>
    /// <param name="isAdmin">Whether the user is an admin.</param>
    /// <returns>The result of the transition attempt.</returns>
    Task<WorkflowTransitionResult> ChangeStatusAsync(int productId, int? storeId, ProductStatus newStatus, int userId, bool isAdmin = false);

    /// <summary>
    /// Allows an admin to override a product's status (e.g., suspend for policy violations).
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="newStatus">The target status.</param>
    /// <param name="adminUserId">The admin user ID performing the override.</param>
    /// <param name="reason">The reason for the override (for audit).</param>
    /// <returns>The result of the override attempt.</returns>
    Task<WorkflowTransitionResult> AdminOverrideStatusAsync(int productId, ProductStatus newStatus, int adminUserId, string reason);
}

/// <summary>
/// Service for managing product workflow state transitions with validation.
/// </summary>
public class ProductWorkflowService : IProductWorkflowService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductWorkflowService> _logger;

    /// <summary>
    /// Defines the allowed workflow transitions for sellers.
    /// Key: Current status, Value: List of allowed target statuses.
    /// </summary>
    private static readonly Dictionary<ProductStatus, HashSet<ProductStatus>> SellerAllowedTransitions = new()
    {
        // Draft -> Active (if valid), Archived
        { ProductStatus.Draft, new HashSet<ProductStatus> { ProductStatus.Active, ProductStatus.Archived } },
        // Active -> Suspended, Archived (cannot go back to Draft)
        { ProductStatus.Active, new HashSet<ProductStatus> { ProductStatus.Suspended, ProductStatus.Archived } },
        // Suspended -> Active (reactivate), Archived
        { ProductStatus.Suspended, new HashSet<ProductStatus> { ProductStatus.Active, ProductStatus.Archived } },
        // Archived -> no transitions allowed for sellers
        { ProductStatus.Archived, new HashSet<ProductStatus>() }
    };

    /// <summary>
    /// Additional transitions allowed for admins.
    /// Key: Current status, Value: List of additional allowed target statuses.
    /// </summary>
    private static readonly Dictionary<ProductStatus, HashSet<ProductStatus>> AdminAdditionalTransitions = new()
    {
        // Admins can revert Active to Draft if needed
        { ProductStatus.Active, new HashSet<ProductStatus> { ProductStatus.Draft } },
        // Admins can un-archive products
        { ProductStatus.Archived, new HashSet<ProductStatus> { ProductStatus.Draft, ProductStatus.Suspended } }
    };

    public ProductWorkflowService(
        ApplicationDbContext context,
        ILogger<ProductWorkflowService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Static helper method to validate product data for activation.
    /// Returns a list of error messages.
    /// </summary>
    /// <param name="product">The product to validate.</param>
    /// <returns>List of validation error messages.</returns>
    public static List<string> ValidateForActivationStatic(Product product)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(product.Title))
        {
            errors.Add("Product title is required.");
        }

        if (string.IsNullOrWhiteSpace(product.Description))
        {
            errors.Add("Product description is required for active products.");
        }

        if (string.IsNullOrWhiteSpace(product.Category))
        {
            errors.Add("Product category is required.");
        }

        if (product.Price <= 0)
        {
            errors.Add("Product price must be greater than zero.");
        }

        if (product.Stock < 0)
        {
            errors.Add("Product stock cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(product.ImageUrls))
        {
            errors.Add("At least one product image is required for active products.");
        }

        return errors;
    }

    /// <summary>
    /// Static helper method to check if a workflow transition is allowed.
    /// </summary>
    /// <param name="currentStatus">The current product status.</param>
    /// <param name="newStatus">The target product status.</param>
    /// <param name="isAdmin">Whether the user is an admin.</param>
    /// <returns>True if the transition is allowed; otherwise false.</returns>
    public static bool IsTransitionAllowedStatic(ProductStatus currentStatus, ProductStatus newStatus, bool isAdmin = false)
    {
        // Same status is always allowed (no-op)
        if (currentStatus == newStatus)
        {
            return true;
        }

        // Check seller allowed transitions
        if (SellerAllowedTransitions.TryGetValue(currentStatus, out var allowedForSeller) 
            && allowedForSeller.Contains(newStatus))
        {
            return true;
        }

        // Check admin additional transitions
        if (isAdmin && AdminAdditionalTransitions.TryGetValue(currentStatus, out var allowedForAdmin) 
            && allowedForAdmin.Contains(newStatus))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Static helper method to get the list of allowed status transitions.
    /// </summary>
    /// <param name="currentStatus">The current product status.</param>
    /// <param name="isAdmin">Whether the user is an admin.</param>
    /// <returns>List of allowed target statuses.</returns>
    public static IEnumerable<ProductStatus> GetAllowedTransitionsStatic(ProductStatus currentStatus, bool isAdmin = false)
    {
        var allowed = new HashSet<ProductStatus>();

        if (SellerAllowedTransitions.TryGetValue(currentStatus, out var sellerTransitions))
        {
            foreach (var status in sellerTransitions)
            {
                allowed.Add(status);
            }
        }

        if (isAdmin && AdminAdditionalTransitions.TryGetValue(currentStatus, out var adminTransitions))
        {
            foreach (var status in adminTransitions)
            {
                allowed.Add(status);
            }
        }

        return allowed;
    }

    /// <inheritdoc />
    public WorkflowTransitionResult ValidateForActivation(Product product)
    {
        var result = new WorkflowTransitionResult { Success = true };

        // Validate required fields for active products
        if (string.IsNullOrWhiteSpace(product.Title))
        {
            result.InvalidFields.Add("Title");
            result.Errors.Add("Product title is required.");
        }

        if (string.IsNullOrWhiteSpace(product.Description))
        {
            result.InvalidFields.Add("Description");
            result.Errors.Add("Product description is required for active products.");
        }

        if (string.IsNullOrWhiteSpace(product.Category))
        {
            result.InvalidFields.Add("Category");
            result.Errors.Add("Product category is required.");
        }

        if (product.Price <= 0)
        {
            result.InvalidFields.Add("Price");
            result.Errors.Add("Product price must be greater than zero.");
        }

        if (product.Stock < 0)
        {
            result.InvalidFields.Add("Stock");
            result.Errors.Add("Product stock cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(product.ImageUrls))
        {
            result.InvalidFields.Add("ImageUrls");
            result.Errors.Add("At least one product image is required for active products.");
        }

        if (result.Errors.Count > 0)
        {
            result.Success = false;
        }

        return result;
    }

    /// <inheritdoc />
    public bool IsTransitionAllowed(ProductStatus currentStatus, ProductStatus newStatus, bool isAdmin = false)
    {
        // Same status is always allowed (no-op)
        if (currentStatus == newStatus)
        {
            return true;
        }

        // Check seller allowed transitions
        if (SellerAllowedTransitions.TryGetValue(currentStatus, out var allowedForSeller) 
            && allowedForSeller.Contains(newStatus))
        {
            return true;
        }

        // Check admin additional transitions
        if (isAdmin && AdminAdditionalTransitions.TryGetValue(currentStatus, out var allowedForAdmin) 
            && allowedForAdmin.Contains(newStatus))
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public IEnumerable<ProductStatus> GetAllowedTransitions(ProductStatus currentStatus, bool isAdmin = false)
    {
        var allowed = new HashSet<ProductStatus>();

        if (SellerAllowedTransitions.TryGetValue(currentStatus, out var sellerTransitions))
        {
            foreach (var status in sellerTransitions)
            {
                allowed.Add(status);
            }
        }

        if (isAdmin && AdminAdditionalTransitions.TryGetValue(currentStatus, out var adminTransitions))
        {
            foreach (var status in adminTransitions)
            {
                allowed.Add(status);
            }
        }

        return allowed;
    }

    /// <inheritdoc />
    public async Task<WorkflowTransitionResult> ChangeStatusAsync(
        int productId, 
        int? storeId, 
        ProductStatus newStatus, 
        int userId, 
        bool isAdmin = false)
    {
        var result = new WorkflowTransitionResult();

        // Get the product
        var query = _context.Products.AsQueryable();
        if (storeId.HasValue)
        {
            query = query.Where(p => p.StoreId == storeId.Value);
        }

        var product = await query.FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null)
        {
            result.Errors.Add("Product not found or you do not have permission to modify it.");
            return result;
        }

        // Check if transition is allowed
        if (!IsTransitionAllowed(product.Status, newStatus, isAdmin))
        {
            result.Errors.Add($"Cannot transition from '{product.Status}' to '{newStatus}'. This transition is not allowed.");
            return result;
        }

        // If transitioning to Active, validate data quality requirements
        if (newStatus == ProductStatus.Active && product.Status != ProductStatus.Active)
        {
            var validationResult = ValidateForActivation(product);
            if (!validationResult.Success)
            {
                result.Errors.AddRange(validationResult.Errors);
                result.InvalidFields.AddRange(validationResult.InvalidFields);
                return result;
            }
        }

        var previousStatus = product.Status;

        // Apply the status change
        product.Status = newStatus;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Product {ProductId} status changed from {PreviousStatus} to {NewStatus} by user {UserId}",
            productId,
            previousStatus,
            newStatus,
            userId);

        result.Success = true;
        result.Product = product;
        return result;
    }

    /// <inheritdoc />
    public async Task<WorkflowTransitionResult> AdminOverrideStatusAsync(
        int productId, 
        ProductStatus newStatus, 
        int adminUserId, 
        string reason)
    {
        var result = new WorkflowTransitionResult();

        if (string.IsNullOrWhiteSpace(reason))
        {
            result.Errors.Add("A reason must be provided for admin override.");
            return result;
        }

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null)
        {
            result.Errors.Add("Product not found.");
            return result;
        }

        var previousStatus = product.Status;

        // Admin can override to any status except they still need to validate for Active
        if (newStatus == ProductStatus.Active)
        {
            var validationResult = ValidateForActivation(product);
            if (!validationResult.Success)
            {
                result.Errors.AddRange(validationResult.Errors);
                result.InvalidFields.AddRange(validationResult.InvalidFields);
                return result;
            }
        }

        // Apply the status change
        product.Status = newStatus;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogWarning(
            "Admin override: Product {ProductId} status changed from {PreviousStatus} to {NewStatus} by admin {AdminUserId}. Reason: {Reason}",
            productId,
            previousStatus,
            newStatus,
            adminUserId,
            reason);

        result.Success = true;
        result.Product = product;
        return result;
    }
}
