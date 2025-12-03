using System.IO.Compression;
using System.Text;
using System.Text.Json;
using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for exporting user personal data.
/// Implements GDPR Right of Access compliance.
/// </summary>
public class DataExportService : IDataExportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataExportService> _logger;
    private readonly IAdminAuditLogService _auditLogService;

    public DataExportService(
        ApplicationDbContext context,
        ILogger<DataExportService> logger,
        IAdminAuditLogService auditLogService)
    {
        _context = context;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<byte[]> GenerateUserDataExportAsync(int userId, string? ipAddress, string? userAgent)
    {
        var exportLog = new DataExportLog
        {
            UserId = userId,
            RequestedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Format = "JSON"
        };

        try
        {
            _logger.LogInformation("Starting data export for user {UserId}", userId);

            // Gather all user data
            var userData = await GatherUserDataAsync(userId);

            // Create ZIP file in memory
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                // Add user profile data
                AddJsonToArchive(archive, "user_profile.json", userData.UserProfile);

                // Add addresses
                if (userData.Addresses.Any())
                {
                    AddJsonToArchive(archive, "addresses.json", userData.Addresses);
                }

                // Add stores (for sellers)
                if (userData.Stores.Any())
                {
                    AddJsonToArchive(archive, "stores.json", userData.Stores);
                }

                // Add orders
                if (userData.Orders.Any())
                {
                    AddJsonToArchive(archive, "orders.json", userData.Orders);
                }

                // Add product reviews
                if (userData.ProductReviews.Any())
                {
                    AddJsonToArchive(archive, "product_reviews.json", userData.ProductReviews);
                }

                // Add seller ratings
                if (userData.SellerRatings.Any())
                {
                    AddJsonToArchive(archive, "seller_ratings.json", userData.SellerRatings);
                }

                // Add consent history
                if (userData.Consents.Any())
                {
                    AddJsonToArchive(archive, "consent_history.json", userData.Consents);
                }

                // Add login events
                if (userData.LoginEvents.Any())
                {
                    AddJsonToArchive(archive, "login_history.json", userData.LoginEvents);
                }

                // Add notifications
                if (userData.Notifications.Any())
                {
                    AddJsonToArchive(archive, "notifications.json", userData.Notifications);
                }

                // Add order messages
                if (userData.OrderMessages.Any())
                {
                    AddJsonToArchive(archive, "order_messages.json", userData.OrderMessages);
                }

                // Add return requests
                if (userData.ReturnRequests.Any())
                {
                    AddJsonToArchive(archive, "return_requests.json", userData.ReturnRequests);
                }

                // Add product questions
                if (userData.ProductQuestions.Any())
                {
                    AddJsonToArchive(archive, "product_questions.json", userData.ProductQuestions);
                }

                // Add analytics events
                if (userData.AnalyticsEvents.Any())
                {
                    AddJsonToArchive(archive, "analytics_events.json", userData.AnalyticsEvents);
                }

                // Add README explaining the export
                AddTextToArchive(archive, "README.txt", GenerateReadmeContent());
            }

            memoryStream.Position = 0;
            var zipBytes = memoryStream.ToArray();

            // Update export log
            exportLog.CompletedAt = DateTime.UtcNow;
            exportLog.FileSizeBytes = zipBytes.Length;
            exportLog.IsSuccessful = true;

            _context.DataExportLogs.Add(exportLog);
            await _context.SaveChangesAsync();

            // Log to audit trail
            await _auditLogService.LogActionAsync(
                adminUserId: userId, // User is requesting their own data
                action: "DataExportRequested",
                entityType: "UserDataExport",
                entityId: exportLog.Id,
                entityDisplayName: $"Data export for user {userId}",
                targetUserId: userId,
                reason: "User exercised GDPR Right of Access");

            _logger.LogInformation(
                "Successfully completed data export for user {UserId}. Export size: {Size} bytes",
                userId,
                zipBytes.Length);

            return zipBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating data export for user {UserId}", userId);

            exportLog.CompletedAt = DateTime.UtcNow;
            exportLog.IsSuccessful = false;
            exportLog.ErrorMessage = ex.Message;

            _context.DataExportLogs.Add(exportLog);
            await _context.SaveChangesAsync();

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<DataExportLog>> GetExportHistoryAsync(int userId, int limit = 10)
    {
        return await _context.DataExportLogs
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.RequestedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<PaginatedList<DataExportLog>> GetAllExportLogsAsync(int page = 1, int pageSize = 50)
    {
        var query = _context.DataExportLogs
            .Include(log => log.User)
            .OrderByDescending(log => log.RequestedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedList<DataExportLog>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    private async Task<UserDataExport> GatherUserDataAsync(int userId)
    {
        var export = new UserDataExport();

        // User profile
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            export.UserProfile = new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.Address,
                user.City,
                user.PostalCode,
                user.Country,
                user.UserType,
                user.Status,
                user.CreatedAt,
                user.KycStatus,
                user.KycSubmittedAt,
                user.KycCompletedAt,
                user.TwoFactorEnabled,
                user.TwoFactorEnabledAt
            };
        }

        // Addresses
        export.Addresses = (await _context.Addresses
            .Where(a => a.UserId == userId)
            .Select(a => new
            {
                a.Id,
                a.FullName,
                a.PhoneNumber,
                a.AddressLine1,
                a.AddressLine2,
                a.City,
                a.StateProvince,
                a.PostalCode,
                a.CountryCode,
                a.IsDefault,
                a.DeliveryInstructions,
                a.CreatedAt,
                a.UpdatedAt
            })
            .ToListAsync()).Cast<object>().ToList();

        // Stores (for sellers)
        export.Stores = (await _context.Stores
            .Where(s => s.UserId == userId)
            .Select(s => new
            {
                s.Id,
                s.StoreName,
                s.Slug,
                s.Description,
                s.Category,
                s.CreatedAt
            })
            .ToListAsync()).Cast<object>().ToList();

        // Orders
        export.Orders = (await _context.Orders
            .Where(o => o.UserId == userId)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.Status,
                o.TotalAmount,
                o.OrderedAt,
                Items = o.Items.Select(i => new
                {
                    i.ProductTitle,
                    i.Quantity,
                    i.UnitPrice,
                    i.Subtotal
                })
            })
            .ToListAsync()).Cast<object>().ToList();

        // Product Reviews
        export.ProductReviews = (await _context.ProductReviews
            .Where(r => r.UserId == userId)
            .Select(r => new
            {
                r.Id,
                r.ProductId,
                r.Rating,
                r.ReviewText,
                r.CreatedAt,
                r.ModerationStatus
            })
            .ToListAsync()).Cast<object>().ToList();

        // Seller Ratings
        export.SellerRatings = (await _context.SellerRatings
            .Where(r => r.UserId == userId)
            .Select(r => new
            {
                r.Id,
                r.StoreId,
                r.SellerSubOrderId,
                r.Rating,
                r.ReviewText,
                r.CreatedAt,
                r.ModerationStatus
            })
            .ToListAsync()).Cast<object>().ToList();

        // Consents
        export.Consents = (await _context.UserConsents
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                c.Id,
                c.ConsentType,
                c.IsGranted,
                c.ConsentedAt,
                c.ConsentVersion,
                c.ConsentText,
                c.ConsentContext,
                c.SupersededAt
            })
            .ToListAsync()).Cast<object>().ToList();

        // Login Events
        export.LoginEvents = (await _context.LoginEvents
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(100) // Limit to last 100 login events
            .Select(e => new
            {
                e.Id,
                e.CreatedAt,
                e.IpAddress,
                e.UserAgent,
                e.IsSuccessful,
                e.FailureReason
            })
            .ToListAsync()).Cast<object>().ToList();

        // Notifications
        export.Notifications = (await _context.Notifications
            .Where(n => n.UserId == userId)
            .Select(n => new
            {
                n.Id,
                n.Type,
                n.Title,
                n.Message,
                n.IsRead,
                n.CreatedAt,
                n.ReadAt
            })
            .ToListAsync()).Cast<object>().ToList();

        // Order Messages
        export.OrderMessages = (await _context.OrderMessages
            .Where(m => m.SenderId == userId)
            .Select(m => new
            {
                m.Id,
                m.OrderId,
                m.Content,
                m.SentAt,
                m.IsFromSeller
            })
            .ToListAsync()).Cast<object>().ToList();

        // Return Requests
        export.ReturnRequests = (await _context.ReturnRequests
            .Where(r => r.BuyerId == userId)
            .Select(r => new
            {
                r.Id,
                r.SubOrderId,
                r.RequestType,
                r.Reason,
                r.Description,
                r.Status,
                r.RequestedAt,
                r.ResolvedAt
            })
            .ToListAsync()).Cast<object>().ToList();

        // Product Questions
        export.ProductQuestions = (await _context.ProductQuestions
            .Where(q => q.BuyerId == userId)
            .Include(q => q.Replies)
            .Select(q => new
            {
                q.Id,
                q.ProductId,
                q.Question,
                q.AskedAt,
                Replies = q.Replies.Select(r => new
                {
                    r.Reply,
                    r.RepliedAt
                }).ToList()
            })
            .ToListAsync()).Cast<object>().ToList();

        // Analytics Events
        export.AnalyticsEvents = (await _context.AnalyticsEvents
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(500) // Limit to last 500 analytics events
            .Select(e => new
            {
                e.Id,
                e.EventType,
                e.CreatedAt,
                e.Metadata
            })
            .ToListAsync()).Cast<object>().ToList();

        return export;
    }

    private void AddJsonToArchive(ZipArchive archive, string fileName, object data)
    {
        var entry = archive.CreateEntry(fileName);
        using var entryStream = entry.Open();
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var bytes = Encoding.UTF8.GetBytes(json);
        entryStream.Write(bytes, 0, bytes.Length);
    }

    private void AddTextToArchive(ZipArchive archive, string fileName, string content)
    {
        var entry = archive.CreateEntry(fileName);
        using var entryStream = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        entryStream.Write(bytes, 0, bytes.Length);
    }

    private string GenerateReadmeContent()
    {
        return @"MERCATO - PERSONAL DATA EXPORT
================================

This archive contains all personal data that Mercato stores about you.
Generated on: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + @" UTC

CONTENTS:
---------
- user_profile.json: Your account information and profile details
- addresses.json: Your saved delivery addresses
- stores.json: Your store information (if you are a seller)
- orders.json: Your order history
- product_reviews.json: Reviews you have written for products
- seller_ratings.json: Ratings you have given to sellers
- consent_history.json: History of your privacy consents
- login_history.json: Your recent login activity (last 100 logins)
- notifications.json: Your notifications
- order_messages.json: Messages you have sent regarding orders
- return_requests.json: Your return/refund requests
- product_questions.json: Questions you have asked about products
- analytics_events.json: Your browsing and interaction history (last 500 events)

DATA FORMAT:
------------
All data is provided in JSON format for easy parsing and readability.

YOUR RIGHTS:
------------
Under GDPR and other data protection regulations, you have the right to:
- Access your personal data (this export)
- Rectify inaccurate data
- Request deletion of your data
- Object to processing of your data
- Data portability

For questions or to exercise your rights, please contact our privacy team.

SECURITY NOTE:
--------------
This export contains sensitive personal information. Please store it securely
and do not share it with unauthorized parties.
";
    }

    private class UserDataExport
    {
        public object? UserProfile { get; set; }
        public List<object> Addresses { get; set; } = new();
        public List<object> Stores { get; set; } = new();
        public List<object> Orders { get; set; } = new();
        public List<object> ProductReviews { get; set; } = new();
        public List<object> SellerRatings { get; set; } = new();
        public List<object> Consents { get; set; } = new();
        public List<object> LoginEvents { get; set; } = new();
        public List<object> Notifications { get; set; } = new();
        public List<object> OrderMessages { get; set; } = new();
        public List<object> ReturnRequests { get; set; } = new();
        public List<object> ProductQuestions { get; set; } = new();
        public List<object> AnalyticsEvents { get; set; } = new();
    }
}
