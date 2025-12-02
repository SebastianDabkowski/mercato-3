namespace MercatoApp.Models;

/// <summary>
/// Represents the reason a review was flagged for moderation.
/// </summary>
public enum ReviewFlagReason
{
    /// <summary>
    /// Review contains inappropriate language or profanity.
    /// </summary>
    InappropriateLanguage,

    /// <summary>
    /// Review appears to be spam or fake.
    /// </summary>
    Spam,

    /// <summary>
    /// Review contains personal information.
    /// </summary>
    PersonalInformation,

    /// <summary>
    /// Review is off-topic or not relevant to the product.
    /// </summary>
    OffTopic,

    /// <summary>
    /// Review contains harassment or threats.
    /// </summary>
    Harassment,

    /// <summary>
    /// Review was manually reported by a user.
    /// </summary>
    UserReported,

    /// <summary>
    /// Review appears fraudulent or fake.
    /// </summary>
    Fraudulent,

    /// <summary>
    /// Review contains abusive or offensive content.
    /// Buyer-facing option for reporting inappropriate reviews.
    /// </summary>
    Abuse,

    /// <summary>
    /// Review contains false or misleading information.
    /// Buyer-facing option for reporting inaccurate reviews.
    /// </summary>
    FalseInformation,

    /// <summary>
    /// Other reason for flagging.
    /// </summary>
    Other
}
