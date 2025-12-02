namespace MercatoApp.Models;

/// <summary>
/// Represents the type of return/complaint request.
/// </summary>
public enum ReturnRequestType
{
    /// <summary>
    /// Standard return request - buyer wants to return item(s).
    /// </summary>
    Return,

    /// <summary>
    /// Complaint/product issue - buyer reporting a problem with item(s).
    /// </summary>
    Complaint
}
