namespace MercatoApp.Models;

/// <summary>
/// Represents the status of a settlement report.
/// </summary>
public enum SettlementStatus
{
    /// <summary>
    /// Settlement is in draft state and can be modified.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Settlement has been finalized and cannot be modified.
    /// </summary>
    Finalized = 1,

    /// <summary>
    /// Settlement has been superseded by a newer version.
    /// </summary>
    Superseded = 2
}
