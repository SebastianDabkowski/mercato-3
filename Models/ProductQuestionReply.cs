using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a reply to a product question, typically from the seller.
/// </summary>
public class ProductQuestionReply
{
    /// <summary>
    /// Gets or sets the unique identifier for the reply.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the question ID this reply belongs to.
    /// </summary>
    public int QuestionId { get; set; }

    /// <summary>
    /// Gets or sets the question (navigation property).
    /// </summary>
    public ProductQuestion Question { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user ID of the person who replied (typically the seller).
    /// </summary>
    public int ReplierId { get; set; }

    /// <summary>
    /// Gets or sets the replier (navigation property).
    /// </summary>
    public User Replier { get; set; } = null!;

    /// <summary>
    /// Gets or sets the reply content.
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Reply { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this reply is from the seller (true) or admin/other (false).
    /// </summary>
    public bool IsFromSeller { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the reply was posted.
    /// </summary>
    public DateTime RepliedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the buyer has read this reply.
    /// </summary>
    public bool IsReadByBuyer { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the reply was read.
    /// </summary>
    public DateTime? ReadAt { get; set; }
}
