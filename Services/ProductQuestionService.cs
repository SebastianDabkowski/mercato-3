using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing product questions and answers.
/// </summary>
public class ProductQuestionService : IProductQuestionService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ProductQuestionService> _logger;

    public ProductQuestionService(
        ApplicationDbContext context,
        INotificationService notificationService,
        ILogger<ProductQuestionService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ProductQuestion>> GetProductQuestionsAsync(int productId, bool includeHidden = false)
    {
        var query = _context.ProductQuestions
            .Include(q => q.Buyer)
            .Include(q => q.Replies)
                .ThenInclude(r => r.Replier)
            .Where(q => q.ProductId == productId);

        if (!includeHidden)
        {
            query = query.Where(q => q.IsVisible);
        }

        return await query
            .OrderByDescending(q => q.AskedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ProductQuestion?> GetQuestionByIdAsync(int questionId)
    {
        return await _context.ProductQuestions
            .Include(q => q.Buyer)
            .Include(q => q.Product)
                .ThenInclude(p => p.Store)
            .Include(q => q.Replies)
                .ThenInclude(r => r.Replier)
            .FirstOrDefaultAsync(q => q.Id == questionId);
    }

    /// <inheritdoc />
    public async Task<ProductQuestion> AskQuestionAsync(int productId, int buyerId, string question)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question cannot be empty.", nameof(question));
        }

        if (question.Length > 2000)
        {
            throw new ArgumentException("Question cannot exceed 2000 characters.", nameof(question));
        }

        // Verify product exists
        var product = await _context.Products
            .Include(p => p.Store)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            throw new InvalidOperationException("Product not found.");
        }

        // Create the question
        var productQuestion = new ProductQuestion
        {
            ProductId = productId,
            BuyerId = buyerId,
            Question = question.Trim(),
            AskedAt = DateTime.UtcNow,
            IsAnswered = false,
            IsVisible = true
        };

        _context.ProductQuestions.Add(productQuestion);
        await _context.SaveChangesAsync();

        // Notify seller about new question
        try
        {
            // Get store owner
            var storeOwner = await _context.StoreUserRoles
                .Where(sur => sur.StoreId == product.StoreId && sur.Role == StoreRole.StoreOwner)
                .Select(sur => sur.UserId)
                .FirstOrDefaultAsync();

            if (storeOwner > 0)
            {
                await _notificationService.CreateNotificationAsync(
                    storeOwner,
                    NotificationType.ProductQuestion,
                    $"New question about {product.Title}",
                    $"/Seller/ProductQuestions?productId={productId}"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification for new product question {QuestionId}", productQuestion.Id);
        }

        return productQuestion;
    }

    /// <inheritdoc />
    public async Task<ProductQuestionReply> ReplyToQuestionAsync(int questionId, int replierId, string reply, bool isFromSeller)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(reply))
        {
            throw new ArgumentException("Reply cannot be empty.", nameof(reply));
        }

        if (reply.Length > 2000)
        {
            throw new ArgumentException("Reply cannot exceed 2000 characters.", nameof(reply));
        }

        // Get the question
        var question = await GetQuestionByIdAsync(questionId);
        if (question == null)
        {
            throw new InvalidOperationException("Question not found.");
        }

        // Authorization: verify replier is the seller or an admin
        if (isFromSeller)
        {
            var hasAccess = await _context.StoreUserRoles
                .AnyAsync(sur => sur.UserId == replierId && sur.StoreId == question.Product.StoreId);

            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("User is not authorized to reply to this question.");
            }
        }

        // Create the reply
        var questionReply = new ProductQuestionReply
        {
            QuestionId = questionId,
            ReplierId = replierId,
            Reply = reply.Trim(),
            IsFromSeller = isFromSeller,
            RepliedAt = DateTime.UtcNow,
            IsReadByBuyer = false
        };

        _context.ProductQuestionReplies.Add(questionReply);

        // Mark question as answered
        question.IsAnswered = true;

        await _context.SaveChangesAsync();

        // Notify buyer about the reply
        try
        {
            await _notificationService.CreateNotificationAsync(
                question.BuyerId,
                NotificationType.ProductQuestionReply,
                $"Your question about {question.Product.Title} was answered",
                $"/Product/{question.ProductId}#question-{questionId}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification for product question reply {ReplyId}", questionReply.Id);
        }

        return questionReply;
    }

    /// <inheritdoc />
    public async Task<List<ProductQuestion>> GetUnansweredQuestionsForStoreAsync(int storeId)
    {
        return await _context.ProductQuestions
            .Include(q => q.Buyer)
            .Include(q => q.Product)
            .Where(q => q.Product.StoreId == storeId && !q.IsAnswered && q.IsVisible)
            .OrderByDescending(q => q.AskedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadRepliesCountAsync(int buyerId)
    {
        return await _context.ProductQuestionReplies
            .Include(r => r.Question)
            .Where(r => r.Question.BuyerId == buyerId && !r.IsReadByBuyer)
            .CountAsync();
    }

    /// <inheritdoc />
    public async Task MarkRepliesAsReadAsync(int questionId, int buyerId)
    {
        // Verify the question belongs to the buyer
        var question = await _context.ProductQuestions
            .FirstOrDefaultAsync(q => q.Id == questionId);

        if (question == null || question.BuyerId != buyerId)
        {
            throw new UnauthorizedAccessException("User is not authorized to mark these replies as read.");
        }

        var unreadReplies = await _context.ProductQuestionReplies
            .Where(r => r.QuestionId == questionId && !r.IsReadByBuyer)
            .ToListAsync();

        foreach (var reply in unreadReplies)
        {
            reply.IsReadByBuyer = true;
            reply.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task HideQuestionAsync(int questionId)
    {
        var question = await _context.ProductQuestions.FindAsync(questionId);
        if (question == null)
        {
            throw new InvalidOperationException("Question not found.");
        }

        question.IsVisible = false;
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task ShowQuestionAsync(int questionId)
    {
        var question = await _context.ProductQuestions.FindAsync(questionId);
        if (question == null)
        {
            throw new InvalidOperationException("Question not found.");
        }

        question.IsVisible = true;
        await _context.SaveChangesAsync();
    }
}
