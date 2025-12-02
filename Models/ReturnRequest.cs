using System.ComponentModel.DataAnnotations;

namespace MercatoApp.Models;

/// <summary>
/// Represents a return request initiated by a buyer for a sub-order or specific items.
/// </summary>
public class ReturnRequest
{
    /// <summary>
    /// Gets or sets the unique identifier for the return request.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the return request number (human-readable).
    /// Format: RTN-{Timestamp}-{Id} (e.g., "RTN-20241202-12345")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ReturnNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the seller sub-order ID that this return request belongs to.
    /// </summary>
    public int SubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the seller sub-order (navigation property).
    /// </summary>
    public SellerSubOrder SubOrder { get; set; } = null!;

    /// <summary>
    /// Gets or sets the buyer/user ID who initiated the return.
    /// </summary>
    public int BuyerId { get; set; }

    /// <summary>
    /// Gets or sets the buyer/user (navigation property).
    /// </summary>
    public User Buyer { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of request (return or complaint).
    /// </summary>
    public ReturnRequestType RequestType { get; set; } = ReturnRequestType.Return;

    /// <summary>
    /// Gets or sets the reason for the return.
    /// </summary>
    public ReturnReason Reason { get; set; }

    /// <summary>
    /// Gets or sets the current status of the return request.
    /// </summary>
    public ReturnStatus Status { get; set; } = ReturnStatus.Requested;

    /// <summary>
    /// Gets or sets the buyer's description/comments for the return.
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the seller's response/notes (optional).
    /// </summary>
    [MaxLength(1000)]
    public string? SellerNotes { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the return was requested.
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the return was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the return was approved (if applicable).
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the return was rejected (if applicable).
    /// </summary>
    public DateTime? RejectedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the return was completed (if applicable).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the amount to be refunded (calculated from items).
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Gets or sets whether this is a full sub-order return or partial item return.
    /// </summary>
    public bool IsFullReturn { get; set; }

    /// <summary>
    /// Gets or sets the type of resolution for this case.
    /// </summary>
    public ResolutionType ResolutionType { get; set; } = ResolutionType.None;

    /// <summary>
    /// Gets or sets the resolution notes provided by the seller.
    /// </summary>
    [MaxLength(2000)]
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// Gets or sets the resolution amount (for partial refunds).
    /// Null for non-refund resolutions or when using the calculated RefundAmount.
    /// </summary>
    public decimal? ResolutionAmount { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the case was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Gets or sets the items being returned (navigation property).
    /// Empty if IsFullReturn is true (all items in sub-order are being returned).
    /// </summary>
    public ICollection<ReturnRequestItem> Items { get; set; } = new List<ReturnRequestItem>();

    /// <summary>
    /// Gets or sets the messages in the return request thread (navigation property).
    /// </summary>
    public ICollection<ReturnRequestMessage> Messages { get; set; } = new List<ReturnRequestMessage>();

    /// <summary>
    /// Gets or sets the associated refund transaction if one has been created.
    /// </summary>
    public RefundTransaction? Refund { get; set; }

    /// <summary>
    /// Gets or sets the escalation reason if the case has been escalated.
    /// </summary>
    public EscalationReason EscalationReason { get; set; } = EscalationReason.None;

    /// <summary>
    /// Gets or sets the date and time when the case was escalated.
    /// </summary>
    public DateTime? EscalatedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who escalated the case (buyer, seller, or admin).
    /// </summary>
    public int? EscalatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the user who escalated the case (navigation property).
    /// </summary>
    public User? EscalatedByUser { get; set; }

    /// <summary>
    /// Gets or sets the admin actions taken on this case (navigation property).
    /// </summary>
    public ICollection<ReturnRequestAdminAction> AdminActions { get; set; } = new List<ReturnRequestAdminAction>();
}
