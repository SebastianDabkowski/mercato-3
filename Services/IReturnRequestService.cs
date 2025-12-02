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
}
