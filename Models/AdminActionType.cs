namespace MercatoApp.Models;

/// <summary>
/// Represents the type of admin action taken on a return/complaint case.
/// </summary>
public enum AdminActionType
{
    /// <summary>
    /// Admin escalated the case for review.
    /// </summary>
    Escalated,

    /// <summary>
    /// Admin overrode seller's decision and enforced a different resolution.
    /// </summary>
    OverrideSellerDecision,

    /// <summary>
    /// Admin enforced a refund (partial or full).
    /// </summary>
    EnforceRefund,

    /// <summary>
    /// Admin closed the case without further action.
    /// </summary>
    CloseWithoutAction,

    /// <summary>
    /// Admin added notes or comments to the case.
    /// </summary>
    AddedNotes,

    /// <summary>
    /// Admin reviewed and approved seller's decision.
    /// </summary>
    ApprovedSellerDecision,

    /// <summary>
    /// Admin marked case for escalation due to SLA breach.
    /// </summary>
    EscalatedSLABreach,

    /// <summary>
    /// Admin manually flagged the case for review.
    /// </summary>
    ManualFlag
}
