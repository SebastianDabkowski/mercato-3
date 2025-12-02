using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing order-specific messages between buyers and sellers.
/// </summary>
public class OrderMessageService : IOrderMessageService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<OrderMessageService> _logger;

    public OrderMessageService(
        ApplicationDbContext context,
        INotificationService notificationService,
        ILogger<OrderMessageService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<OrderMessage>> GetOrderMessagesAsync(int orderId)
    {
        return await _context.OrderMessages
            .Include(m => m.Sender)
            .Where(m => m.OrderId == orderId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<OrderMessage> SendMessageAsync(int orderId, int senderId, string content, bool isFromSeller)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Message content cannot be empty.", nameof(content));
        }

        if (content.Length > 2000)
        {
            throw new ArgumentException("Message cannot exceed 2000 characters.", nameof(content));
        }

        // Get the order and verify authorization
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            throw new InvalidOperationException("Order not found.");
        }

        // Verify sender is either the buyer or a seller with items in this order
        bool isAuthorized = false;

        if (isFromSeller)
        {
            // Check if sender has store access to any products in this order
            var sellerStoreIds = await _context.StoreUserRoles
                .Where(sur => sur.UserId == senderId)
                .Select(sur => sur.StoreId)
                .ToListAsync();

            var orderStoreIds = await _context.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.OrderId == orderId)
                .Select(oi => oi.Product.StoreId)
                .Distinct()
                .ToListAsync();

            isAuthorized = sellerStoreIds.Any(sid => orderStoreIds.Contains(sid));
        }
        else
        {
            // Verify sender is the buyer
            isAuthorized = order.UserId == senderId;
        }

        if (!isAuthorized)
        {
            throw new UnauthorizedAccessException("User is not authorized to send messages for this order.");
        }

        // Create the message
        var message = new OrderMessage
        {
            OrderId = orderId,
            SenderId = senderId,
            Content = content.Trim(),
            IsFromSeller = isFromSeller,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.OrderMessages.Add(message);
        await _context.SaveChangesAsync();

        // Send notification to the recipient
        try
        {
            if (isFromSeller && order.UserId.HasValue)
            {
                // Notify buyer
                await _notificationService.CreateNotificationAsync(
                    order.UserId.Value,
                    NotificationType.OrderMessage,
                    $"New message about order {order.OrderNumber}",
                    $"/Account/OrderDetail/{orderId}"
                );
            }
            else if (!isFromSeller)
            {
                // Notify seller(s)
                var sellerIds = await _context.OrderItems
                    .Include(oi => oi.Product)
                        .ThenInclude(p => p.Store)
                    .Where(oi => oi.OrderId == orderId)
                    .Select(oi => oi.Product.StoreId)
                    .Distinct()
                    .ToListAsync();

                foreach (var storeId in sellerIds)
                {
                    var storeOwner = await _context.StoreUserRoles
                        .Where(sur => sur.StoreId == storeId && sur.Role == StoreRole.StoreOwner)
                        .Select(sur => sur.UserId)
                        .FirstOrDefaultAsync();

                    if (storeOwner > 0)
                    {
                        await _notificationService.CreateNotificationAsync(
                            storeOwner,
                            NotificationType.OrderMessage,
                            $"New message about order {order.OrderNumber}",
                            $"/Seller/OrderDetail/{orderId}"
                        );
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification for order message {MessageId}", message.Id);
        }

        return message;
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadMessageCountAsync(int orderId, int userId, bool isSeller)
    {
        return await _context.OrderMessages
            .Where(m => m.OrderId == orderId && 
                       m.SenderId != userId && 
                       m.IsFromSeller != isSeller && 
                       !m.IsRead)
            .CountAsync();
    }

    /// <inheritdoc />
    public async Task MarkMessagesAsReadAsync(int orderId, int userId, bool isSeller)
    {
        // Get the order to verify authorization
        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            throw new InvalidOperationException("Order not found.");
        }

        // Verify authorization
        bool isAuthorized = false;
        if (isSeller)
        {
            var sellerStoreIds = await _context.StoreUserRoles
                .Where(sur => sur.UserId == userId)
                .Select(sur => sur.StoreId)
                .ToListAsync();

            var orderStoreIds = order.Items.Select(oi => oi.Product.StoreId).Distinct();
            isAuthorized = sellerStoreIds.Any(sid => orderStoreIds.Contains(sid));
        }
        else
        {
            isAuthorized = order.UserId == userId;
        }

        if (!isAuthorized)
        {
            throw new UnauthorizedAccessException("User is not authorized to mark messages for this order.");
        }

        // Mark messages as read (messages sent by the other party)
        var unreadMessages = await _context.OrderMessages
            .Where(m => m.OrderId == orderId && 
                       m.SenderId != userId && 
                       m.IsFromSeller != isSeller && 
                       !m.IsRead)
            .ToListAsync();

        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadMessagesForBuyerAsync(int buyerId)
    {
        var buyerOrderIds = await _context.Orders
            .Where(o => o.UserId == buyerId)
            .Select(o => o.Id)
            .ToListAsync();

        return await _context.OrderMessages
            .Where(m => buyerOrderIds.Contains(m.OrderId) && 
                       m.IsFromSeller && 
                       !m.IsRead)
            .CountAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadMessagesForSellerAsync(int storeId)
    {
        // Get all orders that have items from this store
        var orderIds = await _context.OrderItems
            .Include(oi => oi.Product)
            .Where(oi => oi.Product.StoreId == storeId)
            .Select(oi => oi.OrderId)
            .Distinct()
            .ToListAsync();

        return await _context.OrderMessages
            .Where(m => orderIds.Contains(m.OrderId) && 
                       !m.IsFromSeller && 
                       !m.IsRead)
            .CountAsync();
    }
}
