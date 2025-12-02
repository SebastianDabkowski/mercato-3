namespace MercatoApp.Models;

/// <summary>
/// Represents the reason for a return request.
/// </summary>
public enum ReturnReason
{
    /// <summary>
    /// Item arrived damaged or defective.
    /// </summary>
    Damaged,

    /// <summary>
    /// Wrong item was sent.
    /// </summary>
    WrongItem,

    /// <summary>
    /// Item does not match description.
    /// </summary>
    NotAsDescribed,

    /// <summary>
    /// Buyer changed their mind.
    /// </summary>
    ChangedMind,

    /// <summary>
    /// Item arrived too late.
    /// </summary>
    ArrivedLate,

    /// <summary>
    /// Other reason (buyer should provide description).
    /// </summary>
    Other
}
