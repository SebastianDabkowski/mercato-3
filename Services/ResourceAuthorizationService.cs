using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for resource-level authorization.
/// Provides multi-tenant isolation and ownership validation for resources.
/// </summary>
public class ResourceAuthorizationService : IResourceAuthorizationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ResourceAuthorizationService> _logger;

    public ResourceAuthorizationService(
        ApplicationDbContext context,
        ILogger<ResourceAuthorizationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(ResourceAuthorizationResult Result, int? StoreId)> AuthorizeProductAccessAsync(
        int userId, 
        int productId)
    {
        try
        {
            // Get the product with its store
            var product = await _context.Products
                .Include(p => p.Store)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                _logger.LogWarning(
                    "Product access denied - Product {ProductId} not found for user {UserId}",
                    productId, userId);
                return (ResourceAuthorizationResult.Fail("Product not found."), null);
            }

            // Check if the product's store belongs to the user
            if (product.Store.UserId != userId)
            {
                _logger.LogWarning(
                    "Product access denied - User {UserId} attempted to access product {ProductId} owned by user {OwnerId}",
                    userId, productId, product.Store.UserId);
                return (ResourceAuthorizationResult.Fail("You do not have permission to access this product."), null);
            }

            return (ResourceAuthorizationResult.Success(), product.StoreId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error during product authorization for user {UserId} and product {ProductId}",
                userId, productId);
            return (ResourceAuthorizationResult.Fail("Authorization check failed."), null);
        }
    }

    /// <inheritdoc />
    public async Task<(ResourceAuthorizationResult Result, int? StoreId)> AuthorizeSubOrderAccessAsync(
        int userId, 
        int subOrderId)
    {
        try
        {
            // Get the sub-order with its store
            var subOrder = await _context.SellerSubOrders
                .Include(so => so.Store)
                .FirstOrDefaultAsync(so => so.Id == subOrderId);

            if (subOrder == null)
            {
                _logger.LogWarning(
                    "Sub-order access denied - Sub-order {SubOrderId} not found for user {UserId}",
                    subOrderId, userId);
                return (ResourceAuthorizationResult.Fail("Order not found."), null);
            }

            // Check if the sub-order's store belongs to the user
            if (subOrder.Store.UserId != userId)
            {
                _logger.LogWarning(
                    "Sub-order access denied - User {UserId} attempted to access sub-order {SubOrderId} owned by user {OwnerId}",
                    userId, subOrderId, subOrder.Store.UserId);
                return (ResourceAuthorizationResult.Fail("You do not have permission to access this order."), null);
            }

            return (ResourceAuthorizationResult.Success(), subOrder.StoreId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error during sub-order authorization for user {UserId} and sub-order {SubOrderId}",
                userId, subOrderId);
            return (ResourceAuthorizationResult.Fail("Authorization check failed."), null);
        }
    }

    /// <inheritdoc />
    public async Task<ResourceAuthorizationResult> AuthorizeOrderAccessAsync(int userId, int orderId)
    {
        try
        {
            // Get the order
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                _logger.LogWarning(
                    "Order access denied - Order {OrderId} not found for user {UserId}",
                    orderId, userId);
                return ResourceAuthorizationResult.Fail("Order not found.");
            }

            // Check if the order belongs to the user
            if (order.UserId != userId)
            {
                _logger.LogWarning(
                    "Order access denied - User {UserId} attempted to access order {OrderId} owned by user {OwnerId}",
                    userId, orderId, order.UserId);
                return ResourceAuthorizationResult.Fail("You do not have permission to access this order.");
            }

            return ResourceAuthorizationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error during order authorization for user {UserId} and order {OrderId}",
                userId, orderId);
            return ResourceAuthorizationResult.Fail("Authorization check failed.");
        }
    }

    /// <inheritdoc />
    public async Task<int?> GetStoreIdForSellerAsync(int userId)
    {
        try
        {
            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.UserId == userId);

            return store?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving store for user {UserId}", userId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ResourceAuthorizationResult> ValidateStoreOwnershipAsync(int userId, int storeId)
    {
        try
        {
            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.Id == storeId);

            if (store == null)
            {
                _logger.LogWarning(
                    "Store validation failed - Store {StoreId} not found for user {UserId}",
                    storeId, userId);
                return ResourceAuthorizationResult.Fail("Store not found.");
            }

            if (store.UserId != userId)
            {
                _logger.LogWarning(
                    "Store access denied - User {UserId} attempted to access store {StoreId} owned by user {OwnerId}",
                    userId, storeId, store.UserId);
                return ResourceAuthorizationResult.Fail("You do not have permission to access this store.");
            }

            return ResourceAuthorizationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error during store ownership validation for user {UserId} and store {StoreId}",
                userId, storeId);
            return ResourceAuthorizationResult.Fail("Authorization check failed.");
        }
    }
}
