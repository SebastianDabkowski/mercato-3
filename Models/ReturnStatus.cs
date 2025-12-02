namespace MercatoApp.Models;

/// <summary>
/// Represents the status of a return request in the return lifecycle.
/// </summary>
public enum ReturnStatus
{
    /// <summary>
    /// Return has been requested by the buyer and is awaiting seller review.
    /// </summary>
    Requested,

    /// <summary>
    /// Return has been approved by the seller. Buyer can proceed with return shipment.
    /// </summary>
    Approved,

    /// <summary>
    /// Return has been rejected by the seller.
    /// </summary>
    Rejected,

    /// <summary>
    /// Return has been completed (items received and refund processed).
    /// </summary>
    Completed
}
