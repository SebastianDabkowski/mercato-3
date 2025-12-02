using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for return request management service.
/// </summary>
public interface IReturnRequestService
{
    /// <summary>
    /// Validates if a sub-order is eligible for return initiation.
    /// </summary>
    /// <param name="subOrderId">The sub-order ID.</param>
    /// <param name="buyerId">The buyer ID requesting the return.</param>
    /// <returns>A tuple indicating eligibility and error message if not eligible.</returns>
    Task<(bool IsEligible, string? ErrorMessage)> ValidateReturnEligibilityAsync(int subOrderId, int buyerId);

    /// <summary>
    /// Creates a return request for a sub-order or specific items.
    /// </summary>
    /// <param name="subOrderId">The sub-order ID.</param>
    /// <param name="buyerId">The buyer ID requesting the return.</param>
    /// <param name="requestType">The type of request (return or complaint).</param>
    /// <param name="reason">The reason for the return.</param>
    /// <param name="description">Optional description from the buyer.</param>
    /// <param name="isFullReturn">True if all items are being returned, false for partial return.</param>
    /// <param name="itemQuantities">Dictionary of OrderItemId to quantity for partial returns (optional).</param>
    /// <returns>The created return request.</returns>
    Task<ReturnRequest> CreateReturnRequestAsync(
        int subOrderId,
        int buyerId,
        ReturnRequestType requestType,
        ReturnReason reason,
        string? description,
        bool isFullReturn,
        Dictionary<int, int>? itemQuantities = null);

    /// <summary>
    /// Gets all return requests for a buyer.
    /// </summary>
    /// <param name="buyerId">The buyer ID.</param>
    /// <returns>A list of return requests for the buyer.</returns>
    Task<List<ReturnRequest>> GetReturnRequestsByBuyerAsync(int buyerId);

    /// <summary>
    /// Gets all return requests for a specific sub-order.
    /// </summary>
    /// <param name="subOrderId">The sub-order ID.</param>
    /// <returns>A list of return requests for the sub-order.</returns>
    Task<List<ReturnRequest>> GetReturnRequestsBySubOrderAsync(int subOrderId);

    /// <summary>
    /// Gets all return requests for a seller's store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>A list of return requests for the store.</returns>
    Task<List<ReturnRequest>> GetReturnRequestsByStoreAsync(int storeId);

    /// <summary>
    /// Gets a specific return request by its ID.
    /// </summary>
    /// <param name="returnRequestId">The return request ID.</param>
    /// <returns>The return request, or null if not found.</returns>
    Task<ReturnRequest?> GetReturnRequestByIdAsync(int returnRequestId);

    /// <summary>
    /// Approves a return request.
    /// </summary>
    /// <param name="returnRequestId">The return request ID.</param>
    /// <param name="storeId">The store ID (for authorization).</param>
    /// <param name="sellerNotes">Optional notes from the seller.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> ApproveReturnRequestAsync(int returnRequestId, int storeId, string? sellerNotes = null);

    /// <summary>
    /// Rejects a return request.
    /// </summary>
    /// <param name="returnRequestId">The return request ID.</param>
    /// <param name="storeId">The store ID (for authorization).</param>
    /// <param name="sellerNotes">Required notes explaining rejection reason.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> RejectReturnRequestAsync(int returnRequestId, int storeId, string sellerNotes);

    /// <summary>
    /// Adds a message to a return request thread.
    /// </summary>
    /// <param name="returnRequestId">The return request ID.</param>
    /// <param name="senderId">The user ID of the message sender.</param>
    /// <param name="content">The message content.</param>
    /// <param name="isFromSeller">Whether the message is from the seller.</param>
    /// <returns>The created message, or null if the operation failed.</returns>
    Task<ReturnRequestMessage?> AddMessageAsync(int returnRequestId, int senderId, string content, bool isFromSeller);

    /// <summary>
    /// Marks unread messages as read for a specific user viewing a return request.
    /// </summary>
    /// <param name="returnRequestId">The return request ID.</param>
    /// <param name="userId">The user ID who is reading the messages.</param>
    /// <param name="isSellerViewing">Whether the viewer is the seller.</param>
    /// <returns>The number of messages marked as read.</returns>
    Task<int> MarkMessagesAsReadAsync(int returnRequestId, int userId, bool isSellerViewing);

    /// <summary>
    /// Gets the count of unread messages for a specific return request and viewer.
    /// </summary>
    /// <param name="returnRequestId">The return request ID.</param>
    /// <param name="userId">The user ID who is viewing.</param>
    /// <param name="isSellerViewing">Whether the viewer is the seller.</param>
    /// <returns>The count of unread messages sent by the other party.</returns>
    Task<int> GetUnreadMessageCountAsync(int returnRequestId, int userId, bool isSellerViewing);

    /// <summary>
    /// Resolves a return/complaint case with a specific resolution type.
    /// </summary>
    /// <param name="returnRequestId">The return request ID.</param>
    /// <param name="storeId">The store ID (for authorization).</param>
    /// <param name="resolutionType">The type of resolution.</param>
    /// <param name="resolutionNotes">Required notes explaining the resolution decision.</param>
    /// <param name="resolutionAmount">The refund amount (required for partial refunds).</param>
    /// <param name="initiatedByUserId">The user ID who is resolving the case.</param>
    /// <returns>A tuple indicating success and the return request with refund if created.</returns>
    Task<(bool Success, string? ErrorMessage, ReturnRequest? ReturnRequest)> ResolveReturnCaseAsync(
        int returnRequestId,
        int storeId,
        ResolutionType resolutionType,
        string resolutionNotes,
        decimal? resolutionAmount,
        int initiatedByUserId);

    /// <summary>
    /// Validates whether a case resolution can be changed.
    /// </summary>
    /// <param name="returnRequestId">The return request ID.</param>
    /// <returns>A tuple indicating if the resolution can be changed and an error message if not.</returns>
    Task<(bool CanChange, string? ErrorMessage)> CanChangeResolutionAsync(int returnRequestId);
}
