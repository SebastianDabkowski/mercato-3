namespace MercatoApp.Models;

/// <summary>
/// Represents a shopping cart for a user or anonymous session.
/// </summary>
public class Cart
{
    /// <summary>
    /// Gets or sets the unique identifier for the cart.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID for authenticated users (null for anonymous carts).
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the user (navigation property).
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the session ID for anonymous users (null for authenticated carts).
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the cart was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the cart was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the items in the cart (navigation property).
    /// </summary>
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
