namespace MercatoApp.Models;

/// <summary>
/// Types of notifications that can be sent to users.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Notification about a new order placed.
    /// </summary>
    OrderPlaced,

    /// <summary>
    /// Notification about an order status update.
    /// </summary>
    OrderStatusUpdate,

    /// <summary>
    /// Notification about a shipped order.
    /// </summary>
    OrderShipped,

    /// <summary>
    /// Notification about a delivered order.
    /// </summary>
    OrderDelivered,

    /// <summary>
    /// Notification about a new return request.
    /// </summary>
    ReturnRequest,

    /// <summary>
    /// Notification about a return status update.
    /// </summary>
    ReturnStatusUpdate,

    /// <summary>
    /// Notification about a new payout.
    /// </summary>
    PayoutScheduled,

    /// <summary>
    /// Notification about a payout completion.
    /// </summary>
    PayoutCompleted,

    /// <summary>
    /// Notification about a new message in a return request thread.
    /// </summary>
    NewMessage,

    /// <summary>
    /// Notification about a system update or announcement.
    /// </summary>
    SystemUpdate,

    /// <summary>
    /// Notification about a product review.
    /// </summary>
    ProductReview,

    /// <summary>
    /// Notification about a seller rating.
    /// </summary>
    SellerRating,

    /// <summary>
    /// Notification about a new product question.
    /// </summary>
    ProductQuestion,

    /// <summary>
    /// Notification about a reply to a product question.
    /// </summary>
    ProductQuestionReply,

    /// <summary>
    /// Notification about a new order message.
    /// </summary>
    OrderMessage
}
