namespace MercatoApp.Models;

/// <summary>
/// Represents the frequency of seller payouts.
/// </summary>
public enum PayoutFrequency
{
    /// <summary>
    /// Weekly payouts (every 7 days).
    /// </summary>
    Weekly = 0,

    /// <summary>
    /// Bi-weekly payouts (every 14 days).
    /// </summary>
    BiWeekly = 1,

    /// <summary>
    /// Monthly payouts (once per month).
    /// </summary>
    Monthly = 2
}
