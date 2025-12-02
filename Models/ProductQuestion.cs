using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a question asked by a buyer about a product.
/// </summary>
public class ProductQuestion
{
    /// <summary>
    /// Gets or sets the unique identifier for the question.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the product ID this question is about.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product (navigation property).
    /// </summary>
    public Product Product { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user ID of the buyer who asked the question.
    /// </summary>
    public int BuyerId { get; set; }

    /// <summary>
    /// Gets or sets the buyer (navigation property).
    /// </summary>
    public User Buyer { get; set; } = null!;

    /// <summary>
    /// Gets or sets the question content.
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the question was asked.
    /// </summary>
    public DateTime AskedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether this question has been answered.
    /// </summary>
    public bool IsAnswered { get; set; }

    /// <summary>
    /// Gets or sets whether this question is publicly visible.
    /// Admins can hide questions that violate guidelines.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the replies to this question (navigation property).
    /// </summary>
    public ICollection<ProductQuestionReply> Replies { get; set; } = new List<ProductQuestionReply>();
}
