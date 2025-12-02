using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing return requests.
/// </summary>
public class ReturnRequestService : IReturnRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReturnRequestService> _logger;
    private readonly IConfiguration _configuration;

    // Default return window in days (configurable)
    private const int DefaultReturnWindowDays = 30;

    public ReturnRequestService(
        ApplicationDbContext context,
        ILogger<ReturnRequestService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<(bool IsEligible, string? ErrorMessage)> ValidateReturnEligibilityAsync(int subOrderId, int buyerId)
    {
        // Get the sub-order with parent order and status history
        var subOrder = await _context.SellerSubOrders
            .Include(so => so.ParentOrder)
            .Include(so => so.StatusHistory)
            .Include(so => so.Items)
            .FirstOrDefaultAsync(so => so.Id == subOrderId);

        if (subOrder == null)
        {
            return (false, "Sub-order not found.");
        }

        // Verify the buyer owns this order
        if (subOrder.ParentOrder.UserId != buyerId)
        {
            return (false, "You are not authorized to request a return for this order.");
        }

        // Check if sub-order has been delivered
        if (subOrder.Status != OrderStatus.Delivered)
        {
            return (false, "Returns can only be initiated for delivered orders.");
        }

        // Get the most recent delivered status change to determine delivery date
        var deliveredStatusChange = subOrder.StatusHistory
            .Where(sh => sh.NewStatus == OrderStatus.Delivered)
            .OrderByDescending(sh => sh.ChangedAt)
            .FirstOrDefault();

        DateTime deliveryDate = deliveredStatusChange?.ChangedAt ?? subOrder.UpdatedAt;

        // Check if within return window
        var returnWindowDays = _configuration.GetValue<int?>("ReturnPolicy:ReturnWindowDays") ?? DefaultReturnWindowDays;
        var returnDeadline = deliveryDate.AddDays(returnWindowDays);

        if (DateTime.UtcNow > returnDeadline)
        {
            return (false, $"Return window has expired. Returns must be initiated within {returnWindowDays} days of delivery.");
        }

        // Check if a return has already been requested for this sub-order
        var existingReturn = await _context.ReturnRequests
            .Where(rr => rr.SubOrderId == subOrderId)
            .Where(rr => rr.Status != ReturnStatus.Rejected) // Allow new return if previous was rejected
            .FirstOrDefaultAsync();

        if (existingReturn != null)
        {
            return (false, "A return request has already been submitted for this sub-order.");
        }

        return (true, null);
    }

    /// <inheritdoc />
    public async Task<ReturnRequest> CreateReturnRequestAsync(
        int subOrderId,
        int buyerId,
        ReturnReason reason,
        string? description,
        bool isFullReturn,
        Dictionary<int, int>? itemQuantities = null)
    {
        // Validate eligibility first
        var (isEligible, errorMessage) = await ValidateReturnEligibilityAsync(subOrderId, buyerId);
        if (!isEligible)
        {
            throw new InvalidOperationException(errorMessage ?? "Return request is not eligible.");
        }

        // Get the sub-order with items
        var subOrder = await _context.SellerSubOrders
            .Include(so => so.Items)
            .FirstOrDefaultAsync(so => so.Id == subOrderId);

        if (subOrder == null)
        {
            throw new InvalidOperationException("Sub-order not found.");
        }

        // Calculate refund amount
        decimal refundAmount;
        List<ReturnRequestItem> returnItems = new List<ReturnRequestItem>();

        if (isFullReturn)
        {
            // Full return: refund entire sub-order amount (items + shipping)
            refundAmount = subOrder.TotalAmount;
        }
        else
        {
            // Partial return: validate items and calculate refund
            if (itemQuantities == null || !itemQuantities.Any())
            {
                throw new InvalidOperationException("Item quantities must be provided for partial returns.");
            }

            refundAmount = 0;
            foreach (var itemQty in itemQuantities)
            {
                var orderItem = subOrder.Items.FirstOrDefault(i => i.Id == itemQty.Key);
                if (orderItem == null)
                {
                    throw new InvalidOperationException($"Order item {itemQty.Key} not found in this sub-order.");
                }

                if (itemQty.Value <= 0 || itemQty.Value > orderItem.Quantity)
                {
                    throw new InvalidOperationException($"Invalid quantity for item {orderItem.ProductTitle}. Must be between 1 and {orderItem.Quantity}.");
                }

                var itemRefundAmount = orderItem.UnitPrice * itemQty.Value;
                refundAmount += itemRefundAmount;

                returnItems.Add(new ReturnRequestItem
                {
                    OrderItemId = orderItem.Id,
                    Quantity = itemQty.Value,
                    RefundAmount = itemRefundAmount
                });
            }

            // For partial returns, proportional shipping refund could be added here
            // For simplicity in Phase 1, we only refund item costs for partial returns
        }

        // Generate return number
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var returnNumber = $"RTN-{timestamp}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";

        // Create return request
        var returnRequest = new ReturnRequest
        {
            ReturnNumber = returnNumber,
            SubOrderId = subOrderId,
            BuyerId = buyerId,
            Reason = reason,
            Description = description,
            Status = ReturnStatus.Requested,
            RefundAmount = refundAmount,
            IsFullReturn = isFullReturn,
            Items = returnItems,
            RequestedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ReturnRequests.Add(returnRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Return request {ReturnNumber} created for sub-order {SubOrderId} by buyer {BuyerId}",
            returnNumber,
            subOrderId,
            buyerId);

        return returnRequest;
    }

    /// <inheritdoc />
    public async Task<List<ReturnRequest>> GetReturnRequestsByBuyerAsync(int buyerId)
    {
        return await _context.ReturnRequests
            .Include(rr => rr.SubOrder)
                .ThenInclude(so => so.Store)
            .Include(rr => rr.SubOrder)
                .ThenInclude(so => so.ParentOrder)
            .Include(rr => rr.Items)
                .ThenInclude(ri => ri.OrderItem)
            .Where(rr => rr.BuyerId == buyerId)
            .OrderByDescending(rr => rr.RequestedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<ReturnRequest>> GetReturnRequestsBySubOrderAsync(int subOrderId)
    {
        return await _context.ReturnRequests
            .Include(rr => rr.Buyer)
            .Include(rr => rr.Items)
                .ThenInclude(ri => ri.OrderItem)
            .Where(rr => rr.SubOrderId == subOrderId)
            .OrderByDescending(rr => rr.RequestedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<ReturnRequest>> GetReturnRequestsByStoreAsync(int storeId)
    {
        return await _context.ReturnRequests
            .Include(rr => rr.SubOrder)
                .ThenInclude(so => so.ParentOrder)
            .Include(rr => rr.Buyer)
            .Include(rr => rr.Items)
                .ThenInclude(ri => ri.OrderItem)
            .Where(rr => rr.SubOrder.StoreId == storeId)
            .OrderByDescending(rr => rr.RequestedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ReturnRequest?> GetReturnRequestByIdAsync(int returnRequestId)
    {
        return await _context.ReturnRequests
            .Include(rr => rr.SubOrder)
                .ThenInclude(so => so.Store)
            .Include(rr => rr.SubOrder)
                .ThenInclude(so => so.ParentOrder)
            .Include(rr => rr.Buyer)
            .Include(rr => rr.Items)
                .ThenInclude(ri => ri.OrderItem)
            .FirstOrDefaultAsync(rr => rr.Id == returnRequestId);
    }
}
