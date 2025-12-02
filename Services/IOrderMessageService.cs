using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for order messaging service.
/// </summary>
public interface IOrderMessageService
{
    /// <summary>
    /// Gets all messages for a specific order.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <returns>List of order messages.</returns>
    Task<List<OrderMessage>> GetOrderMessagesAsync(int orderId);

    /// <summary>
    /// Sends a message about an order.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <param name="senderId">The sender's user ID.</param>
    /// <param name="content">The message content.</param>
    /// <param name="isFromSeller">Whether the message is from the seller.</param>
    /// <returns>The created message.</returns>
    Task<OrderMessage> SendMessageAsync(int orderId, int senderId, string content, bool isFromSeller);

    /// <summary>
    /// Gets the count of unread messages for a user on a specific order.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="isSeller">Whether the user is the seller.</param>
    /// <returns>Count of unread messages.</returns>
    Task<int> GetUnreadMessageCountAsync(int orderId, int userId, bool isSeller);

    /// <summary>
    /// Marks messages as read for a user viewing the order.
    /// </summary>
    /// <param name="orderId">The order ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="isSeller">Whether the user is the seller.</param>
    /// <returns>Task representing the operation.</returns>
    Task MarkMessagesAsReadAsync(int orderId, int userId, bool isSeller);

    /// <summary>
    /// Gets the total count of unread messages for a buyer across all orders.
    /// </summary>
    /// <param name="buyerId">The buyer's user ID.</param>
    /// <returns>Count of unread messages.</returns>
    Task<int> GetUnreadMessagesForBuyerAsync(int buyerId);

    /// <summary>
    /// Gets the total count of unread messages for a seller across all orders.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>Count of unread messages.</returns>
    Task<int> GetUnreadMessagesForSellerAsync(int storeId);
}
