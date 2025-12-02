using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing order status transitions.
/// Implements business logic and validation for order lifecycle.
/// </summary>
public class OrderStatusService : IOrderStatusService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderStatusService> _logger;

    public OrderStatusService(
        ApplicationDbContext context,
        ILogger<OrderStatusService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> MarkOrderAsPaidAsync(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.SubOrders)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for payment update", orderId);
            return false;
        }

        // Update order status to Paid
        order.Status = OrderStatus.Paid;
        order.PaymentStatus = PaymentStatus.Completed;
        order.UpdatedAt = DateTime.UtcNow;

        // Update all sub-orders to Paid and log status changes
        foreach (var subOrder in order.SubOrders)
        {
            var previousStatus = subOrder.Status;
            subOrder.Status = OrderStatus.Paid;
            subOrder.UpdatedAt = DateTime.UtcNow;
            
            // Log status change for each sub-order
            await LogStatusChangeAsync(subOrder.Id, previousStatus, OrderStatus.Paid, "Payment completed");
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} marked as paid with {SubOrderCount} sub-orders", 
            orderId, order.SubOrders.Count);

        return true;
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage)> UpdateSubOrderToPreparingAsync(int subOrderId, int? userId = null)
    {
        var subOrder = await _context.SellerSubOrders
            .Include(so => so.ParentOrder)
            .FirstOrDefaultAsync(so => so.Id == subOrderId);

        if (subOrder == null)
        {
            return (false, "Sub-order not found.");
        }

        // Validate status transition
        if (!IsValidStatusTransition(subOrder.Status, OrderStatus.Preparing))
        {
            return (false, $"Cannot change status from {subOrder.Status} to Preparing. Order must be in Paid status.");
        }

        var previousStatus = subOrder.Status;
        subOrder.Status = OrderStatus.Preparing;
        subOrder.UpdatedAt = DateTime.UtcNow;

        // Log status change with user ID
        await LogStatusChangeAsync(subOrderId, previousStatus, OrderStatus.Preparing, userId: userId);

        await _context.SaveChangesAsync();
        await UpdateParentOrderStatusAsync(subOrder.ParentOrderId);

        _logger.LogInformation("Sub-order {SubOrderId} status updated to Preparing by user {UserId}", subOrderId, userId);

        return (true, null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage)> UpdateSubOrderToShippedAsync(
        int subOrderId,
        string? trackingNumber = null,
        string? carrierName = null,
        string? trackingUrl = null,
        int? userId = null)
    {
        var subOrder = await _context.SellerSubOrders
            .Include(so => so.ParentOrder)
            .FirstOrDefaultAsync(so => so.Id == subOrderId);

        if (subOrder == null)
        {
            return (false, "Sub-order not found.");
        }

        // Validate status transition
        if (!IsValidStatusTransition(subOrder.Status, OrderStatus.Shipped))
        {
            return (false, $"Cannot change status from {subOrder.Status} to Shipped. Order must be in Preparing status.");
        }

        var previousStatus = subOrder.Status;
        subOrder.Status = OrderStatus.Shipped;
        subOrder.TrackingNumber = trackingNumber;
        subOrder.CarrierName = carrierName;
        subOrder.TrackingUrl = trackingUrl;
        subOrder.UpdatedAt = DateTime.UtcNow;

        // Log status change with tracking info and user ID
        var notes = trackingNumber != null 
            ? $"Tracking: {trackingNumber}" + (carrierName != null ? $" via {carrierName}" : "") 
            : null;
        await LogStatusChangeAsync(subOrderId, previousStatus, OrderStatus.Shipped, notes, userId);

        await _context.SaveChangesAsync();
        await UpdateParentOrderStatusAsync(subOrder.ParentOrderId);

        _logger.LogInformation("Sub-order {SubOrderId} status updated to Shipped by user {UserId}", subOrderId, userId);

        return (true, null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage)> UpdateSubOrderToDeliveredAsync(int subOrderId, int? userId = null)
    {
        var subOrder = await _context.SellerSubOrders
            .Include(so => so.ParentOrder)
            .FirstOrDefaultAsync(so => so.Id == subOrderId);

        if (subOrder == null)
        {
            return (false, "Sub-order not found.");
        }

        // Validate status transition
        if (!IsValidStatusTransition(subOrder.Status, OrderStatus.Delivered))
        {
            return (false, $"Cannot change status from {subOrder.Status} to Delivered. Order must be in Shipped status.");
        }

        var previousStatus = subOrder.Status;
        subOrder.Status = OrderStatus.Delivered;
        subOrder.UpdatedAt = DateTime.UtcNow;

        // Log status change with user ID
        await LogStatusChangeAsync(subOrderId, previousStatus, OrderStatus.Delivered, userId: userId);

        await _context.SaveChangesAsync();
        await UpdateParentOrderStatusAsync(subOrder.ParentOrderId);

        _logger.LogInformation("Sub-order {SubOrderId} status updated to Delivered by user {UserId}", subOrderId, userId);

        return (true, null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage)> CancelSubOrderAsync(int subOrderId, int? userId = null)
    {
        var subOrder = await _context.SellerSubOrders
            .Include(so => so.ParentOrder)
            .FirstOrDefaultAsync(so => so.Id == subOrderId);

        if (subOrder == null)
        {
            return (false, "Sub-order not found.");
        }

        // Validate status transition
        if (!IsValidStatusTransition(subOrder.Status, OrderStatus.Cancelled))
        {
            return (false, $"Cannot cancel order in {subOrder.Status} status. Order can only be cancelled before shipment.");
        }

        var previousStatus = subOrder.Status;
        subOrder.Status = OrderStatus.Cancelled;
        subOrder.UpdatedAt = DateTime.UtcNow;

        // Log status change with user ID
        await LogStatusChangeAsync(subOrderId, previousStatus, OrderStatus.Cancelled, userId: userId);

        await _context.SaveChangesAsync();
        await UpdateParentOrderStatusAsync(subOrder.ParentOrderId);

        _logger.LogInformation("Sub-order {SubOrderId} cancelled by user {UserId}", subOrderId, userId);

        return (true, null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage)> RefundSubOrderAsync(int subOrderId, decimal refundAmount)
    {
        var subOrder = await _context.SellerSubOrders
            .Include(so => so.ParentOrder)
            .FirstOrDefaultAsync(so => so.Id == subOrderId);

        if (subOrder == null)
        {
            return (false, "Sub-order not found.");
        }

        // Calculate remaining refundable amount
        var remainingRefundableAmount = subOrder.TotalAmount - subOrder.RefundedAmount;

        if (refundAmount <= 0 || refundAmount > remainingRefundableAmount)
        {
            return (false, $"Invalid refund amount. Must be between 0 and {remainingRefundableAmount:C} (remaining refundable amount).");
        }

        // Validate status transition
        if (!IsValidStatusTransition(subOrder.Status, OrderStatus.Refunded))
        {
            return (false, $"Cannot refund order in {subOrder.Status} status.");
        }

        var previousStatus = subOrder.Status;
        subOrder.Status = OrderStatus.Refunded;
        subOrder.RefundedAmount += refundAmount; // Add to existing refunded amount for partial refunds
        subOrder.UpdatedAt = DateTime.UtcNow;

        // Update parent order refunded amount
        var parentOrder = subOrder.ParentOrder;
        parentOrder.RefundedAmount += refundAmount;
        parentOrder.UpdatedAt = DateTime.UtcNow;

        // Log status change with refund amount
        await LogStatusChangeAsync(subOrderId, previousStatus, OrderStatus.Refunded, 
            $"Refund amount: {refundAmount:C}");

        await _context.SaveChangesAsync();
        await UpdateParentOrderStatusAsync(subOrder.ParentOrderId);

        _logger.LogInformation("Sub-order {SubOrderId} refunded with amount {RefundAmount:C}", 
            subOrderId, refundAmount);

        return (true, null);
    }

    /// <inheritdoc />
    public bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        // Same status is always valid (no-op)
        if (currentStatus == newStatus)
        {
            return true;
        }

        // Define valid state transitions
        return (currentStatus, newStatus) switch
        {
            // From New
            (OrderStatus.New, OrderStatus.Paid) => true,
            (OrderStatus.New, OrderStatus.Cancelled) => true,

            // From Paid
            (OrderStatus.Paid, OrderStatus.Preparing) => true,
            (OrderStatus.Paid, OrderStatus.Cancelled) => true,
            (OrderStatus.Paid, OrderStatus.Refunded) => true,

            // From Preparing
            (OrderStatus.Preparing, OrderStatus.Shipped) => true,
            (OrderStatus.Preparing, OrderStatus.Cancelled) => true,

            // From Shipped
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            (OrderStatus.Shipped, OrderStatus.Refunded) => true,

            // From Delivered
            (OrderStatus.Delivered, OrderStatus.Refunded) => true,

            // From Cancelled - no transitions allowed
            (OrderStatus.Cancelled, _) => false,

            // From Refunded - no transitions allowed
            (OrderStatus.Refunded, _) => false,

            // All other transitions are invalid
            _ => false
        };
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage)> UpdateTrackingInformationAsync(
        int subOrderId,
        string? trackingNumber = null,
        string? carrierName = null,
        string? trackingUrl = null,
        int? userId = null)
    {
        var subOrder = await _context.SellerSubOrders
            .FirstOrDefaultAsync(so => so.Id == subOrderId);

        if (subOrder == null)
        {
            return (false, "Sub-order not found.");
        }

        // Can only update tracking info for shipped or delivered orders
        if (subOrder.Status != OrderStatus.Shipped && subOrder.Status != OrderStatus.Delivered)
        {
            return (false, $"Cannot update tracking information for order in {subOrder.Status} status. Order must be shipped or delivered.");
        }

        subOrder.TrackingNumber = trackingNumber;
        subOrder.CarrierName = carrierName;
        subOrder.TrackingUrl = trackingUrl;
        subOrder.UpdatedAt = DateTime.UtcNow;

        // Log the tracking information update (no status change)
        var notes = "Tracking information updated: " + 
            (trackingNumber != null ? $"Tracking: {trackingNumber}" : "") + 
            (carrierName != null ? $" via {carrierName}" : "");
        await LogStatusChangeAsync(subOrderId, subOrder.Status, subOrder.Status, notes.Trim(), userId);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Sub-order {SubOrderId} tracking information updated by user {UserId}", subOrderId, userId);

        return (true, null);
    }

    /// <inheritdoc />
    public async Task UpdateParentOrderStatusAsync(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.SubOrders)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for status update", orderId);
            return;
        }

        // Don't update if there are no sub-orders
        if (!order.SubOrders.Any())
        {
            return;
        }

        var subOrderStatuses = order.SubOrders.Select(so => so.Status).ToList();

        // Determine parent order status based on sub-order statuses
        OrderStatus newStatus;

        if (subOrderStatuses.All(s => s == OrderStatus.Delivered))
        {
            // All delivered
            newStatus = OrderStatus.Delivered;
        }
        else if (subOrderStatuses.All(s => s == OrderStatus.Cancelled))
        {
            // All cancelled
            newStatus = OrderStatus.Cancelled;
        }
        else if (subOrderStatuses.All(s => s == OrderStatus.Refunded))
        {
            // All refunded
            newStatus = OrderStatus.Refunded;
        }
        else if (subOrderStatuses.Any(s => s == OrderStatus.Shipped || s == OrderStatus.Delivered))
        {
            // At least one shipped or delivered (ignore cancelled/refunded for overall status)
            newStatus = OrderStatus.Shipped;
        }
        else if (subOrderStatuses.Any(s => s == OrderStatus.Preparing))
        {
            // At least one preparing (ignore cancelled/refunded)
            newStatus = OrderStatus.Preparing;
        }
        else if (subOrderStatuses.All(s => s == OrderStatus.Paid))
        {
            // All paid
            newStatus = OrderStatus.Paid;
        }
        else if (subOrderStatuses.All(s => s == OrderStatus.New))
        {
            // All new
            newStatus = OrderStatus.New;
        }
        else
        {
            // Mixed states - prioritize active orders over cancelled/refunded
            var activeStatuses = subOrderStatuses
                .Where(s => s != OrderStatus.Cancelled && s != OrderStatus.Refunded)
                .ToList();
            
            if (activeStatuses.Any())
            {
                // Use most advanced active status
                if (activeStatuses.Any(s => s == OrderStatus.Delivered))
                    newStatus = OrderStatus.Delivered;
                else if (activeStatuses.Any(s => s == OrderStatus.Shipped))
                    newStatus = OrderStatus.Shipped;
                else if (activeStatuses.Any(s => s == OrderStatus.Preparing))
                    newStatus = OrderStatus.Preparing;
                else if (activeStatuses.Any(s => s == OrderStatus.Paid))
                    newStatus = OrderStatus.Paid;
                else
                    newStatus = OrderStatus.New;
            }
            else
            {
                // All are cancelled or refunded
                if (subOrderStatuses.All(s => s == OrderStatus.Cancelled))
                    newStatus = OrderStatus.Cancelled;
                else if (subOrderStatuses.All(s => s == OrderStatus.Refunded))
                    newStatus = OrderStatus.Refunded;
                else
                    newStatus = OrderStatus.Cancelled; // Mixed terminal states, default to cancelled
            }
        }

        if (order.Status != newStatus)
        {
            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Parent order {OrderId} status updated to {NewStatus}", 
                orderId, newStatus);
        }
    }

    /// <summary>
    /// Logs a status change for a seller sub-order.
    /// </summary>
    /// <param name="subOrderId">The sub-order ID.</param>
    /// <param name="previousStatus">The previous status (null for initial status).</param>
    /// <param name="newStatus">The new status.</param>
    /// <param name="notes">Optional notes about the status change.</param>
    /// <param name="userId">The user ID making the change (optional, for audit trail).</param>
    private async Task LogStatusChangeAsync(
        int subOrderId, 
        OrderStatus? previousStatus, 
        OrderStatus newStatus, 
        string? notes = null,
        int? userId = null)
    {
        var history = new OrderStatusHistory
        {
            SellerSubOrderId = subOrderId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Notes = notes,
            ChangedByUserId = userId,
            ChangedAt = DateTime.UtcNow
        };

        _context.OrderStatusHistories.Add(history);
    }
}
