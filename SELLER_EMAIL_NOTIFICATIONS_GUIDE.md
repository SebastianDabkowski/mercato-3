# Seller Email Notifications - Quick Reference Guide

## For Developers

### How to Send Seller Notifications

#### 1. New Order Notification
Already integrated in `OrderService.CreateOrderFromCartAsync()`. Automatically sent after order creation.

```csharp
// Email is sent automatically in OrderService
// No additional code needed when creating orders
```

#### 2. Return Request Notification
Already integrated in `ReturnRequestService.CreateReturnRequestAsync()`. Automatically sent after return request creation.

```csharp
// Email is sent automatically in ReturnRequestService
// No additional code needed when creating return requests
```

#### 3. Payout Notification
Already integrated in `PayoutService.ProcessPayoutAsync()`. Automatically sent when payout status becomes `Paid`.

```csharp
// Email is sent automatically in PayoutService
// No additional code needed when processing payouts
```

### Manual Email Sending (Advanced)

If you need to send seller notifications manually:

```csharp
// Inject IEmailService
private readonly IEmailService _emailService;

// New Order Notification
var subOrder = await _context.SellerSubOrders
    .Include(so => so.Store)
        .ThenInclude(s => s.User)
    .Include(so => so.Items)
    .Include(so => so.ParentOrder)
        .ThenInclude(o => o.DeliveryAddress)
    .FirstAsync(so => so.Id == subOrderId);

var order = await _context.Orders
    .Include(o => o.DeliveryAddress)
    .FirstAsync(o => o.Id == orderId);

await _emailService.SendNewOrderNotificationToSellerAsync(subOrder, order);

// Return Request Notification
var returnRequest = await _context.ReturnRequests
    .Include(rr => rr.SubOrder)
        .ThenInclude(so => so.Store)
            .ThenInclude(s => s.User)
    .Include(rr => rr.Buyer)
    .FirstAsync(rr => rr.Id == returnRequestId);

await _emailService.SendReturnRequestNotificationToSellerAsync(returnRequest);

// Payout Notification
var payout = await _context.Payouts
    .Include(p => p.Store)
        .ThenInclude(s => s.User)
    .Include(p => p.PayoutMethod)
    .FirstAsync(p => p.Id == payoutId);

await _emailService.SendPayoutNotificationToSellerAsync(payout);
```

### Checking Email Logs

#### Query Email Logs in Database
```csharp
// Get all seller notifications
var sellerEmails = await _context.EmailLogs
    .Where(e => e.EmailType == EmailType.SellerNewOrder
             || e.EmailType == EmailType.SellerReturnRequest
             || e.EmailType == EmailType.SellerPayout)
    .OrderByDescending(e => e.CreatedAt)
    .ToListAsync();

// Get emails for specific store
var storeEmails = await _context.EmailLogs
    .Include(e => e.SellerSubOrder)
    .Where(e => e.SellerSubOrder.StoreId == storeId)
    .ToListAsync();

// Get failed emails
var failedEmails = await _context.EmailLogs
    .Where(e => e.Status == EmailStatus.Failed)
    .ToListAsync();
```

#### SQL Queries
```sql
-- All seller notifications
SELECT * FROM EmailLogs 
WHERE EmailType IN (8, 9, 10)  -- SellerNewOrder, SellerReturnRequest, SellerPayout
ORDER BY CreatedAt DESC;

-- Failed notifications
SELECT * FROM EmailLogs 
WHERE EmailType IN (8, 9, 10) 
  AND Status = 2  -- Failed
ORDER BY CreatedAt DESC;

-- Notifications for specific store
SELECT el.* 
FROM EmailLogs el
LEFT JOIN SellerSubOrders sso ON el.SellerSubOrderId = sso.Id
WHERE el.EmailType = 8  -- SellerNewOrder
  AND sso.StoreId = 123;
```

### Email Types Reference

| EmailType | Value | Description | Sent When |
|-----------|-------|-------------|-----------|
| SellerNewOrder | 8 | New order notification | Order created and committed |
| SellerReturnRequest | 9 | Return/complaint notification | Return request created |
| SellerPayout | 10 | Payout notification | Payout status = Paid |

### Testing Email Notifications

#### Unit Testing
```csharp
// Mock IEmailService
var mockEmailService = new Mock<IEmailService>();

// Verify email was called
mockEmailService.Verify(
    x => x.SendNewOrderNotificationToSellerAsync(
        It.IsAny<SellerSubOrder>(), 
        It.IsAny<Order>()), 
    Times.Once);
```

#### Integration Testing
```csharp
// Use in-memory database
var context = new ApplicationDbContext(options);
var emailService = new EmailService(context, logger);

// Create test order
var order = CreateTestOrder();
var subOrder = CreateTestSubOrder(order);

// Send notification
await emailService.SendNewOrderNotificationToSellerAsync(subOrder, order);

// Verify email log
var emailLog = await context.EmailLogs
    .FirstOrDefaultAsync(e => e.SellerSubOrderId == subOrder.Id);
Assert.NotNull(emailLog);
Assert.Equal(EmailStatus.Sent, emailLog.Status);
```

## For Store Owners

### Email Configuration

#### Current Implementation
Emails are logged to the database and console. No actual emails are sent yet.

#### Production Setup (Future)
When production email provider is configured:
1. Emails will be sent to `Store.ContactEmail` (if set)
2. Falls back to `Store.User.Email` (store owner's email)
3. Update your contact email in store settings

### Email Content

#### New Order Email
- Subject: "New Order - [Order Number]"
- Contains: Order number, amount, items count, buyer order number, delivery address
- Action: Review order and prepare for fulfillment

#### Return Request Email
- Subject: "Return Request - [Return Number]" or "Complaint - [Complaint Number]"
- Contains: Return number, sub-order, reason, buyer name, requested date
- Action: Review return request and respond within SLA deadline

#### Payout Email
- Subject: "Payout Processed - [Payout Number]"
- Contains: Payout number, amount, currency, status, scheduled date, payment method
- Action: Verify payout received in your account

### Troubleshooting

#### Not Receiving Emails?
1. Check if `Store.ContactEmail` is set in database
2. Check if `Store.User.Email` is valid
3. Query `EmailLogs` table to see if email was sent
4. Check email provider logs (when configured)

#### Email Sent to Wrong Address?
1. Update `Store.ContactEmail` in database
2. OR update store owner's email (`User.Email`)
3. Future: Can configure multiple recipients per store

## Common Issues

### Issue: Email not sent
**Solution**: Check application logs for errors. Email failures are logged but don't block operations.

### Issue: Missing navigation properties
**Solution**: Ensure all required `Include()` statements are present when loading entities.

### Issue: Email sent multiple times
**Solution**: This shouldn't happen as emails are only sent once per event. Check for duplicate event triggers.

## Environment Setup

### Development
```bash
# Build
dotnet build

# Run (starts web app, emails logged to console)
dotnet run

# Check logs
grep "notification would be sent" logs/app.log
```

### Production
```bash
# Configure email provider in appsettings.json
{
  "Email": {
    "Provider": "SendGrid",
    "ApiKey": "your-api-key",
    "FromAddress": "noreply@mercatoapp.com"
  }
}

# Deploy
dotnet publish -c Release
```

## Support

For issues or questions:
1. Check application logs
2. Query EmailLogs table
3. Review SELLER_EMAIL_NOTIFICATIONS.md
4. Contact development team

## Version History

- **v1.0** (2024-12-02): Initial implementation
  - New order notifications
  - Return/complaint notifications
  - Payout notifications
  - Email logging infrastructure
