namespace MercatoApp.Models;

/// <summary>
/// Represents the type of refund.
/// </summary>
public enum RefundType
{
    /// <summary>
    /// Full refund of the entire order amount.
    /// </summary>
    Full,

    /// <summary>
    /// Partial refund of a portion of the order amount.
    /// </summary>
    Partial
}
