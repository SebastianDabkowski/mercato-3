using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a message in a return/complaint request thread.
/// </summary>
public class ReturnRequestMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the message.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the return request ID this message belongs to.
    /// </summary>
    public int ReturnRequestId { get; set; }

    /// <summary>
    /// Gets or sets the return request (navigation property).
    /// </summary>
    public ReturnRequest ReturnRequest { get; set; } = null!;

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
