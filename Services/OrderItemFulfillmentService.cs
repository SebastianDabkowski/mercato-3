using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing order item fulfillment.
/// Handles item-level operations for partial fulfillment within sub-orders.
/// </summary>
public class OrderItemFulfillmentService : IOrderItemFulfillmentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderItemFulfillmentService> _logger;

    public OrderItemFulfillmentService(
        ApplicationDbContext context,
        ILogger<OrderItemFulfillmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage)> ShipItemQuantityAsync(
        int orderItemId,
        int quantityToShip,
        int? userId = null)
    {
        var item = await _context.OrderItems
            .Include(i => i.SellerSubOrder)
            .FirstOrDefaultAsync(i => i.Id == orderItemId);

        if (item == null)
        {
            return (false, "Order item not found.");
        }

        if (quantityToShip <= 0)
        {
            return (false, "Quantity to ship must be greater than 0.");
        }

        // Check available quantity
        var availableQty = item.Quantity - item.QuantityShipped - item.QuantityCancelled;
        if (quantityToShip > availableQty)
        {
            return (false, $"Cannot ship {quantityToShip} items. Only {availableQty} available.");
        }

        // Validate sub-order status
        if (item.SellerSubOrder == null)
        {
            return (false, "Sub-order not found.");
        }

        var (isAllowed, errorMessage) = await ValidateItemFulfillmentAsync(item.SellerSubOrder.Id);
        if (!isAllowed)
        {
            return (false, errorMessage);
        }

        // Update item quantities and status
        item.QuantityShipped += quantityToShip;

        // Update item status based on fulfillment state
        if (item.QuantityShipped == item.Quantity)
        {
            // All items shipped - mark as fully shipped
            item.Status = OrderItemStatus.Shipped;
        }
        else if (item.QuantityShipped > 0)
        {
            // Partial shipment - keep in preparing state
            // Only mark as fully shipped when all quantity is shipped
            if (item.Status == OrderItemStatus.New)
            {
                item.Status = OrderItemStatus.Preparing;
            }
            // If already preparing, stay in preparing until all shipped
        }

        await _context.SaveChangesAsync();

        // Update sub-order status if needed
        await UpdateSubOrderStatusBasedOnItemsAsync(item.SellerSubOrderId!.Value);

        _logger.LogInformation(
            "Shipped {Quantity} units of order item {OrderItemId} by user {UserId}",
            quantityToShip, orderItemId, userId);

        return (true, null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage, decimal RefundAmount)> CancelItemQuantityAsync(
        int orderItemId,
        int quantityToCancel,
        int? userId = null)
    {
        var item = await _context.OrderItems
            .Include(i => i.SellerSubOrder)
            .ThenInclude(so => so!.ParentOrder)
            .FirstOrDefaultAsync(i => i.Id == orderItemId);

        if (item == null)
        {
            return (false, "Order item not found.", 0);
        }

        if (quantityToCancel <= 0)
        {
            return (false, "Quantity to cancel must be greater than 0.", 0);
        }

        // Check available quantity
        var availableQty = item.Quantity - item.QuantityShipped - item.QuantityCancelled;
        if (quantityToCancel > availableQty)
        {
            return (false, $"Cannot cancel {quantityToCancel} items. Only {availableQty} available.", 0);
        }

        // Validate sub-order status (can't cancel already shipped items)
        if (item.SellerSubOrder == null)
        {
            return (false, "Sub-order not found.", 0);
        }

        // Calculate refund amount for cancelled quantity
        var refundAmount = await CalculateItemRefundAmountAsync(orderItemId, quantityToCancel);

        // Update item quantities and status
        item.QuantityCancelled += quantityToCancel;
        item.RefundedAmount += refundAmount;

        // Update item status based on cancellation state
        if (item.QuantityCancelled == item.Quantity)
        {
            // All items cancelled
            item.Status = OrderItemStatus.Cancelled;
        }
        else if (item.QuantityCancelled > 0)
        {
            // Partial cancellation - maintain current status if preparing/shipped
            // If new, mark as preparing since we're taking action
            if (item.Status == OrderItemStatus.New)
            {
                item.Status = OrderItemStatus.Preparing;
            }
        }

        // Update sub-order refunded amount
        item.SellerSubOrder.RefundedAmount += refundAmount;

        // Update parent order refunded amount
        if (item.SellerSubOrder.ParentOrder != null)
        {
            item.SellerSubOrder.ParentOrder.RefundedAmount += refundAmount;
            item.SellerSubOrder.ParentOrder.UpdatedAt = DateTime.UtcNow;
        }

        item.SellerSubOrder.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Update sub-order status if needed
        await UpdateSubOrderStatusBasedOnItemsAsync(item.SellerSubOrderId!.Value);

        _logger.LogInformation(
            "Cancelled {Quantity} units of order item {OrderItemId} with refund {RefundAmount:C} by user {UserId}",
            quantityToCancel, orderItemId, refundAmount, userId);

        return (true, null, refundAmount);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage)> MarkItemAsPreparingAsync(
        int orderItemId,
        int? userId = null)
    {
        var item = await _context.OrderItems
            .Include(i => i.SellerSubOrder)
            .FirstOrDefaultAsync(i => i.Id == orderItemId);

        if (item == null)
        {
            return (false, "Order item not found.");
        }

        if (item.Status != OrderItemStatus.New)
        {
            return (false, $"Cannot change status from {item.Status} to Preparing. Item must be in New status.");
        }

        // Validate sub-order status
        if (item.SellerSubOrder == null)
        {
            return (false, "Sub-order not found.");
        }

        var (isAllowed, errorMessage) = await ValidateItemFulfillmentAsync(item.SellerSubOrder.Id);
        if (!isAllowed)
        {
            return (false, errorMessage);
        }

        item.Status = OrderItemStatus.Preparing;

        await _context.SaveChangesAsync();

        // Update sub-order status if needed
        await UpdateSubOrderStatusBasedOnItemsAsync(item.SellerSubOrderId!.Value);

        _logger.LogInformation(
            "Marked order item {OrderItemId} as preparing by user {UserId}",
            orderItemId, userId);

        return (true, null);
    }

    /// <inheritdoc />
    public async Task<int> GetAvailableQuantityAsync(int orderItemId)
    {
        var item = await _context.OrderItems
            .FirstOrDefaultAsync(i => i.Id == orderItemId);

        if (item == null)
        {
            return 0;
        }

        return item.Quantity - item.QuantityShipped - item.QuantityCancelled;
    }

    /// <inheritdoc />
    public async Task<(bool IsAllowed, string? ErrorMessage)> ValidateItemFulfillmentAsync(int subOrderId)
    {
        var subOrder = await _context.SellerSubOrders
            .Include(so => so.ParentOrder)
            .FirstOrDefaultAsync(so => so.Id == subOrderId);

        if (subOrder == null)
        {
            return (false, "Sub-order not found.");
        }

        // Can only fulfill items if payment is completed
        if (subOrder.ParentOrder?.PaymentStatus != PaymentStatus.Completed)
        {
            return (false, "Cannot fulfill items until payment is completed.");
        }

        // Cannot fulfill items if sub-order is already in terminal state
        if (subOrder.Status == OrderStatus.Cancelled || subOrder.Status == OrderStatus.Refunded)
        {
            return (false, $"Cannot fulfill items for sub-order in {subOrder.Status} status.");
        }

        return (true, null);
    }

    /// <inheritdoc />
    public async Task<decimal> CalculateItemRefundAmountAsync(int orderItemId, int quantityToCancel)
    {
        var item = await _context.OrderItems
            .FirstOrDefaultAsync(i => i.Id == orderItemId);

        if (item == null)
        {
            return 0;
        }

        // Calculate proportional refund: (unit price + tax per unit) * quantity
        var taxPerUnit = item.Quantity > 0 ? item.TaxAmount / item.Quantity : 0;
        var refundPerUnit = item.UnitPrice + taxPerUnit;
        var refundAmount = refundPerUnit * quantityToCancel;

        return refundAmount;
    }

    /// <inheritdoc />
    public async Task<List<OrderItem>> GetSubOrderItemsWithStatusAsync(int subOrderId)
    {
        return await _context.OrderItems
            .Include(i => i.Product)
            .Include(i => i.ProductVariant)
            .Where(i => i.SellerSubOrderId == subOrderId)
            .OrderBy(i => i.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Updates the sub-order status based on the aggregated item statuses.
    /// Determines the overall sub-order status from individual item states.
    /// </summary>
    /// <param name="subOrderId">The sub-order ID.</param>
    private async Task UpdateSubOrderStatusBasedOnItemsAsync(int subOrderId)
    {
        var subOrder = await _context.SellerSubOrders
            .Include(so => so.Items)
            .FirstOrDefaultAsync(so => so.Id == subOrderId);

        if (subOrder == null || !subOrder.Items.Any())
        {
            return;
        }

        var items = subOrder.Items.ToList();

        // Determine sub-order status based on item states
        var allItemStatuses = items.Select(i => i.Status).ToList();

        // If all items are cancelled, mark sub-order as cancelled
        if (allItemStatuses.All(s => s == OrderItemStatus.Cancelled))
        {
            if (subOrder.Status != OrderStatus.Cancelled)
            {
                subOrder.Status = OrderStatus.Cancelled;
                subOrder.UpdatedAt = DateTime.UtcNow;
            }
        }
        // If all items are shipped, mark sub-order as shipped
        else if (allItemStatuses.All(s => s == OrderItemStatus.Shipped))
        {
            if (subOrder.Status != OrderStatus.Shipped && subOrder.Status != OrderStatus.Delivered)
            {
                subOrder.Status = OrderStatus.Shipped;
                subOrder.UpdatedAt = DateTime.UtcNow;
            }
        }
        // If any items are shipped (partial fulfillment), mark sub-order as shipped
        else if (allItemStatuses.Any(s => s == OrderItemStatus.Shipped))
        {
            if (subOrder.Status != OrderStatus.Shipped && subOrder.Status != OrderStatus.Delivered)
            {
                subOrder.Status = OrderStatus.Shipped;
                subOrder.UpdatedAt = DateTime.UtcNow;
            }
        }
        // If any items are preparing, mark sub-order as preparing
        else if (allItemStatuses.Any(s => s == OrderItemStatus.Preparing))
        {
            if (subOrder.Status != OrderStatus.Preparing && 
                subOrder.Status != OrderStatus.Shipped && 
                subOrder.Status != OrderStatus.Delivered)
            {
                subOrder.Status = OrderStatus.Preparing;
                subOrder.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
    }
}
