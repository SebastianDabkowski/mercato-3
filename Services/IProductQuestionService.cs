using MercatoApp.Models;

namespace MercatoApp.Services;

/// <summary>
/// Interface for product question service.
/// </summary>
public interface IProductQuestionService
{
    /// <summary>
    /// Gets questions for a specific product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="includeHidden">Whether to include hidden questions (for admins).</param>
    /// <returns>List of product questions.</returns>
    Task<List<ProductQuestion>> GetProductQuestionsAsync(int productId, bool includeHidden = false);

    /// <summary>
    /// Gets a specific question by ID.
    /// </summary>
    /// <param name="questionId">The question ID.</param>
    /// <returns>The question or null if not found.</returns>
    Task<ProductQuestion?> GetQuestionByIdAsync(int questionId);

    /// <summary>
    /// Asks a question about a product.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="buyerId">The buyer's user ID.</param>
    /// <param name="question">The question content.</param>
    /// <returns>The created question.</returns>
    Task<ProductQuestion> AskQuestionAsync(int productId, int buyerId, string question);

    /// <summary>
    /// Replies to a product question.
    /// </summary>
    /// <param name="questionId">The question ID.</param>
    /// <param name="replierId">The user ID of the person replying.</param>
    /// <param name="reply">The reply content.</param>
    /// <param name="isFromSeller">Whether the reply is from the seller.</param>
    /// <returns>The created reply.</returns>
    Task<ProductQuestionReply> ReplyToQuestionAsync(int questionId, int replierId, string reply, bool isFromSeller);

    /// <summary>
    /// Gets unanswered questions for a specific store (for seller notification).
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>List of unanswered questions.</returns>
    Task<List<ProductQuestion>> GetUnansweredQuestionsForStoreAsync(int storeId);

    /// <summary>
    /// Gets the count of unread replies for a buyer.
    /// </summary>
    /// <param name="buyerId">The buyer's user ID.</param>
    /// <returns>Count of unread replies.</returns>
    Task<int> GetUnreadRepliesCountAsync(int buyerId);

    /// <summary>
    /// Marks all replies to a question as read by the buyer.
    /// </summary>
    /// <param name="questionId">The question ID.</param>
    /// <param name="buyerId">The buyer's user ID.</param>
    /// <returns>Task representing the operation.</returns>
    Task MarkRepliesAsReadAsync(int questionId, int buyerId);

    /// <summary>
    /// Hides a question (admin moderation).
    /// </summary>
    /// <param name="questionId">The question ID.</param>
    /// <returns>Task representing the operation.</returns>
    Task HideQuestionAsync(int questionId);

    /// <summary>
    /// Shows a hidden question (admin moderation).
    /// </summary>
    /// <param name="questionId">The question ID.</param>
    /// <returns>Task representing the operation.</returns>
    Task ShowQuestionAsync(int questionId);
}
