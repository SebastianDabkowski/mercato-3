using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a private message between buyer and seller about a specific order.
/// </summary>
public class OrderMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the message.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the order ID this message is about.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Gets or sets the order (navigation property).
    /// </summary>
    public Order Order { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user ID of the message sender.
    /// </summary>
    public int SenderId { get; set; }

    /// <summary>
    /// Gets or sets the sender (navigation property).
    /// </summary>
    public User Sender { get; set; } = null!;

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this message is from the seller (true) or buyer (false).
    /// </summary>
    public bool IsFromSeller { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the message was sent.
    /// </summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether this message has been read by the recipient.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the message was read.
    /// </summary>
    public DateTime? ReadAt { get; set; }
}
