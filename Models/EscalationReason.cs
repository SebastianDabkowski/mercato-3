namespace MercatoApp.Models;

/// <summary>
/// Represents the reason for escalating a return/complaint case to admin review.
/// </summary>
public enum EscalationReason
{
    /// <summary>
    /// No escalation.
    /// </summary>
    None,

    /// <summary>
    /// Buyer requested escalation after disagreeing with seller's resolution.
    /// </summary>
    BuyerRequested,

    /// <summary>
    /// System automatically escalated due to SLA breach (e.g., seller not responding within timeframe).
    /// </summary>
    SLABreach,

    /// <summary>
    /// Admin manually flagged the case for review.
    /// </summary>
    AdminManualFlag,

    /// <summary>
    /// Platform rules or policies were violated.
    /// </summary>
    PolicyViolation,

    /// <summary>
    /// Buyer and seller cannot reach agreement.
    /// </summary>
    CannotReachAgreement
}
