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
    private readonly IRefundService _refundService;
    private readonly ISLAService _slaService;
    private readonly IEmailService _emailService;
    private readonly int _returnWindowDays;

    // Default return window in days (configurable)
    private const int DefaultReturnWindowDays = 30;

    public ReturnRequestService(
        ApplicationDbContext context,
        ILogger<ReturnRequestService> logger,
        IRefundService refundService,
        ISLAService slaService,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _refundService = refundService;
        _slaService = slaService;
        _emailService = emailService;
        _returnWindowDays = configuration.GetValue<int?>("ReturnPolicy:ReturnWindowDays") ?? DefaultReturnWindowDays;
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
        var returnDeadline = deliveryDate.AddDays(_returnWindowDays);

        if (DateTime.UtcNow > returnDeadline)
        {
            return (false, $"Return window has expired. Returns must be initiated within {_returnWindowDays} days of delivery.");
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
        ReturnRequestType requestType,
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
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(so => so.Id == subOrderId);

        if (subOrder == null)
        {
            throw new InvalidOperationException("Sub-order not found.");
        }

        // Determine category ID for SLA calculation (use first item's category if available)
        // Note: For mixed-category orders, this uses the first item's category as a simplification.
        // Future enhancement: Could use a primary category or most common category in the order.
        int? categoryId = subOrder.Items.FirstOrDefault()?.Product?.CategoryId;

        // Calculate SLA deadlines
        var requestedAt = DateTime.UtcNow;
        var (firstResponseDeadline, resolutionDeadline) = await _slaService.CalculateSLADeadlinesAsync(
            requestedAt,
            categoryId,
            requestType);

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
        var timestamp = requestedAt.ToString("yyyyMMdd");
        var prefix = requestType == ReturnRequestType.Complaint ? "CMP" : "RTN";
        var returnNumber = $"{prefix}-{timestamp}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        // Create return request
        var returnRequest = new ReturnRequest
        {
            ReturnNumber = returnNumber,
            SubOrderId = subOrderId,
            BuyerId = buyerId,
            RequestType = requestType,
            Reason = reason,
            Description = description,
            Status = ReturnStatus.Requested,
            RefundAmount = refundAmount,
            IsFullReturn = isFullReturn,
            Items = returnItems,
            RequestedAt = requestedAt,
            UpdatedAt = requestedAt,
            FirstResponseDeadline = firstResponseDeadline,
            ResolutionDeadline = resolutionDeadline,
            FirstResponseSLABreached = false,
            ResolutionSLABreached = false
        };

        _context.ReturnRequests.Add(returnRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Return request {ReturnNumber} created for sub-order {SubOrderId} by buyer {BuyerId}",
            returnNumber,
            subOrderId,
            buyerId);

        // Send email notification to seller
        try
        {
            // Reload return request with navigation properties for email
            var returnRequestWithDetails = await _context.ReturnRequests
                .Include(rr => rr.SubOrder)
                    .ThenInclude(so => so.Store)
                        .ThenInclude(s => s.User)
                .Include(rr => rr.Buyer)
                .FirstOrDefaultAsync(rr => rr.Id == returnRequest.Id);

            if (returnRequestWithDetails != null)
            {
                await _emailService.SendReturnRequestNotificationToSellerAsync(returnRequestWithDetails);
            }
        }
        catch (Exception ex)
        {
            // Don't fail the return request creation if email notification fails
            _logger.LogError(ex, "Failed to send seller notification for return request {ReturnNumber}", returnNumber);
        }

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
            .Include(rr => rr.SubOrder)
                .ThenInclude(so => so.Items)
            .Include(rr => rr.Buyer)
            .Include(rr => rr.Items)
                .ThenInclude(ri => ri.OrderItem)
            .Include(rr => rr.Messages)
                .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(rr => rr.Id == returnRequestId);
    }

    /// <inheritdoc />
    public async Task<bool> ApproveReturnRequestAsync(int returnRequestId, int storeId, string? sellerNotes = null)
    {
        var returnRequest = await _context.ReturnRequests
            .Include(rr => rr.SubOrder)
            .FirstOrDefaultAsync(rr => rr.Id == returnRequestId);

        if (returnRequest == null)
        {
            _logger.LogWarning("Return request {ReturnRequestId} not found", returnRequestId);
            return false;
        }

        // Verify the store owns this return request
        if (returnRequest.SubOrder.StoreId != storeId)
        {
            _logger.LogWarning("Store {StoreId} attempted to approve return request {ReturnRequestId} belonging to store {ActualStoreId}",
                storeId, returnRequestId, returnRequest.SubOrder.StoreId);
            return false;
        }

        // Only allow approval if status is Requested
        if (returnRequest.Status != ReturnStatus.Requested)
        {
            _logger.LogWarning("Return request {ReturnRequestId} cannot be approved. Current status: {Status}",
                returnRequestId, returnRequest.Status);
            return false;
        }

        // Update status
        returnRequest.Status = ReturnStatus.Approved;
        returnRequest.ApprovedAt = DateTime.UtcNow;
        returnRequest.UpdatedAt = DateTime.UtcNow;
        
        // Record seller's first response if not already set
        if (!returnRequest.SellerFirstResponseAt.HasValue)
        {
            returnRequest.SellerFirstResponseAt = DateTime.UtcNow;
        }
        
        if (!string.IsNullOrWhiteSpace(sellerNotes))
        {
            returnRequest.SellerNotes = sellerNotes;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Return request {ReturnRequestId} approved by store {StoreId}",
            returnRequestId, storeId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RejectReturnRequestAsync(int returnRequestId, int storeId, string sellerNotes)
    {
        if (string.IsNullOrWhiteSpace(sellerNotes))
        {
            throw new ArgumentException("Seller notes are required when rejecting a return request.", nameof(sellerNotes));
        }

        var returnRequest = await _context.ReturnRequests
            .Include(rr => rr.SubOrder)
            .FirstOrDefaultAsync(rr => rr.Id == returnRequestId);

        if (returnRequest == null)
        {
            _logger.LogWarning("Return request {ReturnRequestId} not found", returnRequestId);
            return false;
        }

        // Verify the store owns this return request
        if (returnRequest.SubOrder.StoreId != storeId)
        {
            _logger.LogWarning("Store {StoreId} attempted to reject return request {ReturnRequestId} belonging to store {ActualStoreId}",
                storeId, returnRequestId, returnRequest.SubOrder.StoreId);
            return false;
        }

        // Only allow rejection if status is Requested
        if (returnRequest.Status != ReturnStatus.Requested)
        {
            _logger.LogWarning("Return request {ReturnRequestId} cannot be rejected. Current status: {Status}",
                returnRequestId, returnRequest.Status);
            return false;
        }

        // Update status
        returnRequest.Status = ReturnStatus.Rejected;
        returnRequest.RejectedAt = DateTime.UtcNow;
        returnRequest.UpdatedAt = DateTime.UtcNow;
        returnRequest.SellerNotes = sellerNotes;
        
        // Record seller's first response if not already set
        if (!returnRequest.SellerFirstResponseAt.HasValue)
        {
            returnRequest.SellerFirstResponseAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Return request {ReturnRequestId} rejected by store {StoreId}",
            returnRequestId, storeId);

        return true;
    }

    /// <inheritdoc />
    public async Task<ReturnRequestMessage?> AddMessageAsync(int returnRequestId, int senderId, string content, bool isFromSeller)
    {
        // Validate content
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Message content cannot be empty.", nameof(content));
        }

        if (content.Length > 2000)
        {
            throw new ArgumentException("Message content cannot exceed 2000 characters.", nameof(content));
        }

        // Get the return request with related data
        var returnRequest = await _context.ReturnRequests
            .Include(rr => rr.SubOrder)
            .FirstOrDefaultAsync(rr => rr.Id == returnRequestId);

        if (returnRequest == null)
        {
            _logger.LogWarning("Return request {ReturnRequestId} not found when adding message", returnRequestId);
            return null;
        }

        // Authorization check: verify the sender is either the buyer or the seller
        if (isFromSeller)
        {
            // Verify the sender owns the store
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == returnRequest.SubOrder.StoreId);
            if (store == null || store.UserId != senderId)
            {
                _logger.LogWarning("User {SenderId} attempted to send seller message for return request {ReturnRequestId} but does not own the store",
                    senderId, returnRequestId);
                return null;
            }
        }
        else
        {
            // Verify the sender is the buyer
            if (returnRequest.BuyerId != senderId)
            {
                _logger.LogWarning("User {SenderId} attempted to send buyer message for return request {ReturnRequestId} but is not the buyer",
                    senderId, returnRequestId);
                return null;
            }
        }

        // Create the message
        var message = new ReturnRequestMessage
        {
            ReturnRequestId = returnRequestId,
            SenderId = senderId,
            Content = content,
            IsFromSeller = isFromSeller,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.ReturnRequestMessages.Add(message);

        // Update the return request's UpdatedAt timestamp
        returnRequest.UpdatedAt = DateTime.UtcNow;
        
        // Record seller's first response if this is from the seller and not already set
        if (isFromSeller && !returnRequest.SellerFirstResponseAt.HasValue)
        {
            returnRequest.SellerFirstResponseAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Message added to return request {ReturnRequestId} by user {SenderId} (seller: {IsFromSeller})",
            returnRequestId, senderId, isFromSeller);

        // Reload the message with sender information
        return await _context.ReturnRequestMessages
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == message.Id);
    }

    /// <inheritdoc />
    public async Task<int> MarkMessagesAsReadAsync(int returnRequestId, int userId, bool isSellerViewing)
    {
        // Get the return request
        var returnRequest = await _context.ReturnRequests
            .Include(rr => rr.SubOrder)
            .FirstOrDefaultAsync(rr => rr.Id == returnRequestId);

        if (returnRequest == null)
        {
            _logger.LogWarning("Return request {ReturnRequestId} not found when marking messages as read", returnRequestId);
            return 0;
        }

        // Authorization check: verify the user is either the buyer or the seller
        if (isSellerViewing)
        {
            // Verify the user owns the store
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == returnRequest.SubOrder.StoreId);
            if (store == null || store.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to mark messages as read for return request {ReturnRequestId} but does not own the store",
                    userId, returnRequestId);
                return 0;
            }
        }
        else
        {
            // Verify the user is the buyer
            if (returnRequest.BuyerId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to mark messages as read for return request {ReturnRequestId} but is not the buyer",
                    userId, returnRequestId);
                return 0;
            }
        }

        // Get unread messages that were sent by the other party
        var messagesToMarkAsRead = await _context.ReturnRequestMessages
            .Where(m => m.ReturnRequestId == returnRequestId)
            .Where(m => !m.IsRead)
            .Where(m => m.IsFromSeller != isSellerViewing) // Mark as read messages from the other party
            .ToListAsync();

        if (!messagesToMarkAsRead.Any())
        {
            return 0;
        }

        // Mark messages as read
        foreach (var message in messagesToMarkAsRead)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("{Count} messages marked as read for return request {ReturnRequestId}",
            messagesToMarkAsRead.Count, returnRequestId);

        return messagesToMarkAsRead.Count;
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadMessageCountAsync(int returnRequestId, int userId, bool isSellerViewing)
    {
        // Get the return request for authorization check
        var returnRequest = await _context.ReturnRequests
            .Include(rr => rr.SubOrder)
            .FirstOrDefaultAsync(rr => rr.Id == returnRequestId);

        if (returnRequest == null)
        {
            _logger.LogWarning("Return request {ReturnRequestId} not found when getting unread count", returnRequestId);
            return 0;
        }

        // Authorization check: verify the user is either the buyer or the seller
        if (isSellerViewing)
        {
            // Verify the user owns the store
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == returnRequest.SubOrder.StoreId);
            if (store == null || store.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to get unread count for return request {ReturnRequestId} but does not own the store",
                    userId, returnRequestId);
                return 0;
            }
        }
        else
        {
            // Verify the user is the buyer
            if (returnRequest.BuyerId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to get unread count for return request {ReturnRequestId} but is not the buyer",
                    userId, returnRequestId);
                return 0;
            }
        }

        // Get unread messages that were sent by the other party
        var unreadCount = await _context.ReturnRequestMessages
            .Where(m => m.ReturnRequestId == returnRequestId)
            .Where(m => !m.IsRead)
            .Where(m => m.IsFromSeller != isSellerViewing) // Count messages from the other party
            .CountAsync();

        return unreadCount;
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage, ReturnRequest? ReturnRequest)> ResolveReturnCaseAsync(
        int returnRequestId,
        int storeId,
        ResolutionType resolutionType,
        string resolutionNotes,
        decimal? resolutionAmount,
        int initiatedByUserId)
    {
        // Validate resolution notes
        if (string.IsNullOrWhiteSpace(resolutionNotes))
        {
            return (false, "Resolution notes are required.", null);
        }

        if (resolutionNotes.Length > 2000)
        {
            return (false, "Resolution notes cannot exceed 2000 characters.", null);
        }

        // Get the return request with related data
        var returnRequest = await _context.ReturnRequests
            .Include(rr => rr.SubOrder)
                .ThenInclude(so => so.ParentOrder)
            .Include(rr => rr.Refund)
            .FirstOrDefaultAsync(rr => rr.Id == returnRequestId);

        if (returnRequest == null)
        {
            return (false, "Return request not found.", null);
        }

        // Verify the store owns this return request
        if (returnRequest.SubOrder.StoreId != storeId)
        {
            _logger.LogWarning("Store {StoreId} attempted to resolve return request {ReturnRequestId} belonging to store {ActualStoreId}",
                storeId, returnRequestId, returnRequest.SubOrder.StoreId);
            return (false, "You are not authorized to resolve this return request.", null);
        }

        // Check if resolution can be changed
        var (canChange, errorMessage) = await CanChangeResolutionAsync(returnRequestId);
        if (!canChange)
        {
            return (false, errorMessage, null);
        }

        // Validate resolution amount for partial refunds
        if (resolutionType == ResolutionType.PartialRefund)
        {
            if (!resolutionAmount.HasValue || resolutionAmount.Value <= 0)
            {
                return (false, "Resolution amount is required for partial refunds and must be greater than zero.", null);
            }

            if (resolutionAmount.Value > returnRequest.RefundAmount)
            {
                return (false, $"Partial refund amount cannot exceed the maximum refundable amount of {returnRequest.RefundAmount:C}.", null);
            }
        }

        // Update return request with resolution
        returnRequest.ResolutionType = resolutionType;
        returnRequest.ResolutionNotes = resolutionNotes;
        returnRequest.ResolutionAmount = resolutionAmount;
        returnRequest.Status = ReturnStatus.Resolved;
        returnRequest.ResolvedAt = DateTime.UtcNow;
        returnRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Return request {ReturnRequestId} resolved with {ResolutionType} by store {StoreId}",
            returnRequestId, resolutionType, storeId);

        // Process refund if applicable
        RefundTransaction? refund = null;
        if (resolutionType == ResolutionType.FullRefund || resolutionType == ResolutionType.PartialRefund)
        {
            try
            {
                decimal amountToRefund = resolutionType == ResolutionType.FullRefund
                    ? returnRequest.RefundAmount
                    : resolutionAmount!.Value;

                // Create refund linked to this return request
                refund = await _refundService.ProcessPartialRefundAsync(
                    orderId: returnRequest.SubOrder.ParentOrderId,
                    sellerSubOrderId: returnRequest.SubOrderId,
                    refundAmount: amountToRefund,
                    reason: $"Return case {returnRequest.ReturnNumber} resolution: {resolutionType}",
                    initiatedByUserId: initiatedByUserId,
                    notes: resolutionNotes,
                    returnRequestId: returnRequestId);

                _logger.LogInformation(
                    "Refund {RefundNumber} created for return request {ReturnRequestId} with amount {Amount}",
                    refund.RefundNumber, returnRequestId, amountToRefund);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create refund for return request {ReturnRequestId}", returnRequestId);
                return (false, "Case resolved, but refund initiation failed. Please contact support.", returnRequest);
            }
        }

        // Reload to include the refund relationship
        if (refund != null)
        {
            returnRequest = await GetReturnRequestByIdAsync(returnRequestId);
        }

        return (true, null, returnRequest);
    }

    /// <inheritdoc />
    public async Task<(bool CanChange, string? ErrorMessage)> CanChangeResolutionAsync(int returnRequestId)
    {
        var returnRequest = await _context.ReturnRequests
            .Include(rr => rr.Refund)
            .FirstOrDefaultAsync(rr => rr.Id == returnRequestId);

        if (returnRequest == null)
        {
            return (false, "Return request not found.");
        }

        // Cannot change if status is Completed or Rejected
        if (returnRequest.Status == ReturnStatus.Completed)
        {
            return (false, "Cannot change resolution for completed cases.");
        }

        if (returnRequest.Status == ReturnStatus.Rejected)
        {
            return (false, "Cannot change resolution for rejected cases.");
        }

        // Cannot change if refund has already been processed (status is Completed)
        if (returnRequest.Refund != null && returnRequest.Refund.Status == RefundStatus.Completed)
        {
            return (false, "Cannot change resolution after refund has been completed.");
        }

        return (true, null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage)> EscalateReturnCaseAsync(
        int returnRequestId,
        EscalationReason escalationReason,
        int escalatedByUserId,
        string? adminNotes = null)
    {
        var returnRequest = await _context.ReturnRequests
            .Include(rr => rr.SubOrder)
                .ThenInclude(so => so.Store)
            .Include(rr => rr.Buyer)
            .FirstOrDefaultAsync(rr => rr.Id == returnRequestId);

        if (returnRequest == null)
        {
            return (false, "Return request not found.");
        }

        // Validate that case is in a state that can be escalated
        if (returnRequest.Status == ReturnStatus.UnderAdminReview)
        {
            return (false, "Case is already under admin review.");
        }

        if (returnRequest.Status == ReturnStatus.Completed)
        {
            return (false, "Cannot escalate a completed case.");
        }

        var previousStatus = returnRequest.Status;

        // Update the return request with escalation details
        returnRequest.Status = ReturnStatus.UnderAdminReview;
        returnRequest.EscalationReason = escalationReason;
        returnRequest.EscalatedAt = DateTime.UtcNow;
        returnRequest.EscalatedByUserId = escalatedByUserId;
        returnRequest.UpdatedAt = DateTime.UtcNow;

        // Create an admin action record if notes provided
        if (!string.IsNullOrWhiteSpace(adminNotes))
        {
            var adminAction = new ReturnRequestAdminAction
            {
                ReturnRequestId = returnRequestId,
                AdminUserId = escalatedByUserId,
                ActionType = escalationReason == EscalationReason.SLABreach
                    ? AdminActionType.EscalatedSLABreach
                    : escalationReason == EscalationReason.AdminManualFlag
                        ? AdminActionType.ManualFlag
                        : AdminActionType.Escalated,
                PreviousStatus = previousStatus,
                NewStatus = ReturnStatus.UnderAdminReview,
                Notes = adminNotes,
                ActionTakenAt = DateTime.UtcNow,
                NotificationsSent = false
            };
            _context.ReturnRequestAdminActions.Add(adminAction);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Return request {ReturnRequestId} escalated by user {UserId} with reason {Reason}",
            returnRequestId, escalatedByUserId, escalationReason);

        return (true, null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage, ReturnRequest? ReturnRequest)> RecordAdminDecisionAsync(
        int returnRequestId,
        int adminUserId,
        AdminActionType actionType,
        string notes,
        ReturnStatus? newStatus = null,
        ResolutionType? resolutionType = null,
        decimal? resolutionAmount = null)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return (false, "Admin decision notes are required.", null);
        }

        var returnRequest = await _context.ReturnRequests
            .Include(rr => rr.SubOrder)
                .ThenInclude(so => so.Store)
            .Include(rr => rr.Buyer)
            .Include(rr => rr.Items)
                .ThenInclude(ri => ri.OrderItem)
            .FirstOrDefaultAsync(rr => rr.Id == returnRequestId);

        if (returnRequest == null)
        {
            return (false, "Return request not found.", null);
        }

        var previousStatus = returnRequest.Status;

        // Create the admin action record
        var adminAction = new ReturnRequestAdminAction
        {
            ReturnRequestId = returnRequestId,
            AdminUserId = adminUserId,
            ActionType = actionType,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Notes = notes,
            ResolutionType = resolutionType,
            ResolutionAmount = resolutionAmount,
            ActionTakenAt = DateTime.UtcNow,
            NotificationsSent = false
        };

        _context.ReturnRequestAdminActions.Add(adminAction);

        // Apply the decision based on action type
        switch (actionType)
        {
            case AdminActionType.OverrideSellerDecision:
            case AdminActionType.EnforceRefund:
                if (resolutionType.HasValue)
                {
                    returnRequest.ResolutionType = resolutionType.Value;
                    returnRequest.ResolutionNotes = notes;
                    returnRequest.ResolvedAt = DateTime.UtcNow;
                    returnRequest.Status = ReturnStatus.Resolved;

                    // Create refund if applicable
                    if (resolutionType.Value == ResolutionType.FullRefund || resolutionType.Value == ResolutionType.PartialRefund)
                    {
                        decimal refundAmount = resolutionType.Value == ResolutionType.FullRefund
                            ? returnRequest.RefundAmount
                            : resolutionAmount ?? 0;

                        if (refundAmount > 0)
                        {
                            returnRequest.ResolutionAmount = refundAmount;

                            // Create refund transaction
                            try
                            {
                                await _refundService.ProcessPartialRefundAsync(
                                    returnRequest.SubOrder.ParentOrderId,
                                    returnRequest.BuyerId,
                                    refundAmount,
                                    $"Admin-enforced refund for {returnRequest.ReturnNumber}: {notes}",
                                    returnRequestId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to create refund for admin decision on return {ReturnRequestId}", returnRequestId);
                                return (false, $"Failed to create refund: {ex.Message}", null);
                            }
                        }
                    }
                }
                else if (newStatus.HasValue)
                {
                    returnRequest.Status = newStatus.Value;
                }
                break;

            case AdminActionType.CloseWithoutAction:
                returnRequest.Status = ReturnStatus.Resolved;
                returnRequest.ResolutionType = ResolutionType.NoRefund;
                returnRequest.ResolutionNotes = notes;
                returnRequest.ResolvedAt = DateTime.UtcNow;
                break;

            case AdminActionType.ApprovedSellerDecision:
                // Keep current resolution, just move out of admin review
                returnRequest.Status = ReturnStatus.Resolved;
                if (returnRequest.ResolvedAt == null)
                {
                    returnRequest.ResolvedAt = DateTime.UtcNow;
                }
                break;

            case AdminActionType.AddedNotes:
                // No status change, just adding notes
                break;

            default:
                if (newStatus.HasValue)
                {
                    returnRequest.Status = newStatus.Value;
                }
                break;
        }

        returnRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Admin {AdminUserId} recorded decision {ActionType} on return request {ReturnRequestId}",
            adminUserId, actionType, returnRequestId);

        return (true, null, returnRequest);
    }

    /// <inheritdoc />
    public async Task<List<ReturnRequestAdminAction>> GetAdminActionsAsync(int returnRequestId)
    {
        return await _context.ReturnRequestAdminActions
            .Include(a => a.AdminUser)
            .Where(a => a.ReturnRequestId == returnRequestId)
            .OrderByDescending(a => a.ActionTakenAt)
            .ToListAsync();
    }
}
