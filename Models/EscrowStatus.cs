namespace MercatoApp.Models;

/// <summary>
/// Represents the status of an escrow transaction.
/// </summary>
public enum EscrowStatus
{
    /// <summary>
    /// Funds are held in escrow, awaiting release conditions.
    /// </summary>
    Held = 0,

    /// <summary>
    /// Funds are eligible for payout based on marketplace policy.
    /// </summary>
    EligibleForPayout = 1,

    /// <summary>
    /// Funds have been released to the seller.
    /// </summary>
    Released = 2,

    /// <summary>
    /// Funds have been returned to the buyer (order cancelled/refunded).
    /// </summary>
    ReturnedToBuyer = 3,

    /// <summary>
    /// Escrow is in dispute (e.g., return request pending).
    /// </summary>
    InDispute = 4,

    /// <summary>
    /// Escrow has been partially refunded.
    /// </summary>
    PartiallyRefunded = 5
}
