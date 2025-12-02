namespace MercatoApp.Models;

/// <summary>
/// Represents the type of resolution for a return or complaint case.
/// </summary>
public enum ResolutionType
{
    /// <summary>
    /// No resolution has been determined yet.
    /// </summary>
    None,

    /// <summary>
    /// Full refund of the entire sub-order amount.
    /// </summary>
    FullRefund,

    /// <summary>
    /// Partial refund (less than full amount).
    /// </summary>
    PartialRefund,

    /// <summary>
    /// Item will be replaced with a new one.
    /// </summary>
    Replacement,

    /// <summary>
    /// Item will be repaired.
    /// </summary>
    Repair,

    /// <summary>
    /// No refund or compensation will be provided.
    /// </summary>
    NoRefund
}
