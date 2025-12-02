namespace MercatoApp.Models;

/// <summary>
/// Status of a commission invoice.
/// </summary>
public enum CommissionInvoiceStatus
{
    /// <summary>
    /// Invoice is in draft state and not yet issued.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Invoice has been issued to the seller.
    /// </summary>
    Issued = 1,

    /// <summary>
    /// Invoice has been paid by the seller.
    /// </summary>
    Paid = 2,

    /// <summary>
    /// Invoice has been cancelled.
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Invoice has been superseded by a corrected version.
    /// </summary>
    Superseded = 4
}
