namespace MercatoApp.Models;

/// <summary>
/// Represents the reason why a user account was blocked.
/// </summary>
public enum BlockReason
{
    /// <summary>
    /// Account was blocked due to fraudulent activity.
    /// </summary>
    Fraud,

    /// <summary>
    /// Account was blocked for spamming.
    /// </summary>
    Spam,

    /// <summary>
    /// Account was blocked for violating platform policies.
    /// </summary>
    PolicyViolation,

    /// <summary>
    /// Account was blocked for abusive behavior.
    /// </summary>
    AbusiveBehavior,

    /// <summary>
    /// Account was blocked for other reasons.
    /// </summary>
    Other
}
