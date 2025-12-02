using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing seller payouts.
/// Handles payout scheduling, aggregation, processing, and retry logic.
/// </summary>
public class PayoutService : IPayoutService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PayoutService> _logger;
    private readonly IPayoutSettingsService _payoutSettingsService;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _configuration;

    public PayoutService(
        ApplicationDbContext context,
        ILogger<PayoutService> logger,
        IPayoutSettingsService payoutSettingsService,
        IEmailService emailService,
        INotificationService notificationService,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _payoutSettingsService = payoutSettingsService;
        _emailService = emailService;
        _notificationService = notificationService;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<PayoutSchedule> CreateOrUpdatePayoutScheduleAsync(
        int storeId,
        PayoutFrequency frequency,
        decimal minimumThreshold,
        int? dayOfWeek = null,
        int? dayOfMonth = null)
    {
        // Validate store exists
        var storeExists = await _context.Stores.AnyAsync(s => s.Id == storeId);
        if (!storeExists)
        {
            throw new InvalidOperationException($"Store with ID {storeId} not found.");
        }

        // Validate frequency-specific parameters
        if (frequency == PayoutFrequency.Weekly || frequency == PayoutFrequency.BiWeekly)
        {
            if (!dayOfWeek.HasValue || dayOfWeek.Value < 0 || dayOfWeek.Value > 6)
            {
                throw new ArgumentException("Day of week must be between 0 (Sunday) and 6 (Saturday) for weekly/bi-weekly payouts.");
            }
        }
        else if (frequency == PayoutFrequency.Monthly)
        {
            if (!dayOfMonth.HasValue || dayOfMonth.Value < 1 || dayOfMonth.Value > 28)
            {
                throw new ArgumentException("Day of month must be between 1 and 28 for monthly payouts.");
            }
        }

        // Get or create schedule
        var schedule = await _context.PayoutSchedules
            .FirstOrDefaultAsync(s => s.StoreId == storeId);

        var now = DateTime.UtcNow;
        var isNew = schedule == null;

        if (isNew)
        {
            schedule = new PayoutSchedule
            {
                StoreId = storeId,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.PayoutSchedules.Add(schedule);
        }

        // Ensure schedule is not null at this point
        if (schedule == null)
        {
            throw new InvalidOperationException("Failed to initialize payout schedule.");
        }

        // Update schedule properties
        schedule.Frequency = frequency;
        schedule.MinimumPayoutThreshold = minimumThreshold;
        schedule.DayOfWeek = dayOfWeek;
        schedule.DayOfMonth = dayOfMonth;
        schedule.UpdatedAt = now;

        // Calculate next payout date
        schedule.NextPayoutDate = CalculateNextPayoutDate(frequency, dayOfWeek, dayOfMonth, now);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "{Action} payout schedule for store {StoreId}: {Frequency}, threshold {Threshold}, next payout {NextPayoutDate}",
            isNew ? "Created" : "Updated",
            storeId,
            frequency,
            minimumThreshold,
            schedule.NextPayoutDate);

        return schedule;
    }

    /// <inheritdoc />
    public async Task<PayoutSchedule?> GetPayoutScheduleAsync(int storeId)
    {
        return await _context.PayoutSchedules
            .FirstOrDefaultAsync(s => s.StoreId == storeId);
    }

    /// <inheritdoc />
    public async Task<PayoutBalanceSummary> GetEligibleBalanceSummaryAsync(int storeId)
    {
        var schedule = await GetPayoutScheduleAsync(storeId);
        var minimumThreshold = schedule?.MinimumPayoutThreshold ?? 50.00m;

        var now = DateTime.UtcNow;

        // Get all escrow transactions eligible for payout
        var eligibleEscrows = await _context.EscrowTransactions
            .Where(e => e.StoreId == storeId
                && e.Status == EscrowStatus.EligibleForPayout
                && e.EligibleForPayoutAt <= now
                && e.PayoutId == null) // Not already included in a payout
            .ToListAsync();

        var totalBalance = eligibleEscrows.Sum(e => e.NetAmount - e.RefundedAmount);

        return new PayoutBalanceSummary
        {
            StoreId = storeId,
            EligibleBalance = totalBalance,
            EligibleTransactionCount = eligibleEscrows.Count,
            MeetsThreshold = totalBalance >= minimumThreshold,
            MinimumThreshold = minimumThreshold
        };
    }

    /// <inheritdoc />
    public async Task<PayoutResult> CreatePayoutAsync(int storeId, DateTime scheduledDate)
    {
        var result = new PayoutResult();

        // Validate store exists
        var store = await _context.Stores.FindAsync(storeId);
        if (store == null)
        {
            result.Errors.Add($"Store with ID {storeId} not found.");
            return result;
        }

        // Check payout configuration
        var isConfigComplete = await _payoutSettingsService.IsPayoutConfigurationCompleteAsync(storeId);
        if (!isConfigComplete)
        {
            result.Errors.Add("Payout configuration is incomplete. Please configure a payout method.");
            return result;
        }

        // Get default payout method
        var payoutMethod = await _payoutSettingsService.GetDefaultPayoutMethodAsync(storeId);
        if (payoutMethod == null)
        {
            result.Errors.Add("No default payout method configured.");
            return result;
        }

        // Get eligible balance
        var balanceSummary = await GetEligibleBalanceSummaryAsync(storeId);

        // Check if there are eligible escrows
        if (balanceSummary.EligibleTransactionCount == 0)
        {
            result.Errors.Add("No eligible escrow transactions found.");
            return result;
        }

        // Check minimum threshold
        if (!balanceSummary.MeetsThreshold)
        {
            result.Errors.Add($"Balance {balanceSummary.EligibleBalance:C} is below minimum threshold {balanceSummary.MinimumThreshold:C}. Balance will roll over to next payout.");
            _logger.LogInformation(
                "Payout creation skipped for store {StoreId}: Balance {Balance} below threshold {Threshold}",
                storeId,
                balanceSummary.EligibleBalance,
                balanceSummary.MinimumThreshold);
            return result;
        }

        // Get the payout schedule
        var schedule = await GetPayoutScheduleAsync(storeId);

        // Get all eligible escrow transactions
        var now = DateTime.UtcNow;
        var eligibleEscrows = await _context.EscrowTransactions
            .Where(e => e.StoreId == storeId
                && e.Status == EscrowStatus.EligibleForPayout
                && e.EligibleForPayoutAt <= now
                && e.PayoutId == null)
            .ToListAsync();

        // Get configuration values
        var maxRetryAttempts = _configuration.GetValue<int>("Payout:MaxRetryAttempts", 3);

        // Create the payout
        var payout = new Payout
        {
            PayoutNumber = GeneratePayoutNumber(),
            StoreId = storeId,
            PayoutMethodId = payoutMethod.Id,
            PayoutScheduleId = schedule?.Id,
            Amount = balanceSummary.EligibleBalance,
            Currency = payoutMethod.Currency ?? "USD",
            Status = PayoutStatus.Scheduled,
            ScheduledDate = scheduledDate,
            MaxRetryAttempts = maxRetryAttempts,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Payouts.Add(payout);
        await _context.SaveChangesAsync();

        // Link escrow transactions to this payout
        foreach (var escrow in eligibleEscrows)
        {
            escrow.PayoutId = payout.Id;
            escrow.UpdatedAt = now;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created payout {PayoutNumber} for store {StoreId}: {Amount} {Currency}, {TransactionCount} transactions",
            payout.PayoutNumber,
            storeId,
            payout.Amount,
            payout.Currency,
            eligibleEscrows.Count);

        // Create notification for seller
        try
        {
            await _notificationService.CreateNotificationAsync(
                store.UserId,
                NotificationType.PayoutScheduled,
                "Payout Scheduled",
                $"Payout #{payout.PayoutNumber} of ${payout.Amount:N2} has been scheduled for {scheduledDate:MMM dd, yyyy}.",
                $"/Seller/Payouts/Details/{payout.Id}",
                payout.Id,
                "Payout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification for payout {PayoutNumber}", payout.PayoutNumber);
            // Don't fail payout creation if notification fails
        }

        result.Success = true;
        result.Payout = payout;
        return result;
    }

    /// <inheritdoc />
    public async Task<int> GenerateScheduledPayoutsAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        // Find all enabled schedules that are due
        var dueSchedules = await _context.PayoutSchedules
            .Where(s => s.IsEnabled && s.NextPayoutDate.Date <= today)
            .Include(s => s.Store)
            .ToListAsync();

        var createdCount = 0;

        foreach (var schedule in dueSchedules)
        {
            try
            {
                var result = await CreatePayoutAsync(schedule.StoreId, schedule.NextPayoutDate);

                if (result.Success)
                {
                    createdCount++;

                    // Update next payout date
                    schedule.NextPayoutDate = CalculateNextPayoutDate(
                        schedule.Frequency,
                        schedule.DayOfWeek,
                        schedule.DayOfMonth,
                        now);
                    schedule.UpdatedAt = now;
                }
                else
                {
                    // Log errors but continue processing other schedules
                    _logger.LogWarning(
                        "Failed to create scheduled payout for store {StoreId}: {Errors}",
                        schedule.StoreId,
                        string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating scheduled payout for store {StoreId}",
                    schedule.StoreId);
            }
        }

        if (createdCount > 0)
        {
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation(
            "Generated {CreatedCount} scheduled payouts from {TotalSchedules} due schedules",
            createdCount,
            dueSchedules.Count);

        return createdCount;
    }

    /// <inheritdoc />
    public async Task<int> ProcessScheduledPayoutsAsync()
    {
        var now = DateTime.UtcNow;

        // Find all scheduled payouts that are due
        var duePayouts = await _context.Payouts
            .Where(p => p.Status == PayoutStatus.Scheduled && p.ScheduledDate <= now)
            .Include(p => p.PayoutMethod)
            .ToListAsync();

        var processedCount = 0;

        foreach (var payout in duePayouts)
        {
            try
            {
                var result = await ProcessPayoutAsync(payout.Id);
                if (result.Success)
                {
                    processedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing scheduled payout {PayoutId}",
                    payout.Id);
            }
        }

        _logger.LogInformation(
            "Processed {ProcessedCount} scheduled payouts from {TotalPayouts} due payouts",
            processedCount,
            duePayouts.Count);

        return processedCount;
    }

    /// <inheritdoc />
    public async Task<PayoutResult> ProcessPayoutAsync(int payoutId)
    {
        var result = new PayoutResult();

        var payout = await _context.Payouts
            .Include(p => p.PayoutMethod)
            .Include(p => p.Store)
            .FirstOrDefaultAsync(p => p.Id == payoutId);

        if (payout == null)
        {
            result.Errors.Add($"Payout with ID {payoutId} not found.");
            return result;
        }

        // Only process scheduled or failed payouts
        if (payout.Status != PayoutStatus.Scheduled && payout.Status != PayoutStatus.Failed)
        {
            result.Errors.Add($"Payout {payout.PayoutNumber} has status {payout.Status} and cannot be processed.");
            return result;
        }

        var now = DateTime.UtcNow;

        try
        {
            // Update status to processing
            payout.Status = PayoutStatus.Processing;
            payout.InitiatedAt = now;
            payout.UpdatedAt = now;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Processing payout {PayoutNumber} for store {StoreId}: {Amount} {Currency}",
                payout.PayoutNumber,
                payout.StoreId,
                payout.Amount,
                payout.Currency);

            // Simulate payout processing
            // In a real implementation, this would integrate with a payment provider
            // to transfer funds to the seller's bank account
            var processResult = await SimulatePayoutProcessingAsync(payout);

            if (processResult.success)
            {
                // Mark as paid
                payout.Status = PayoutStatus.Paid;
                payout.CompletedAt = now;
                payout.ExternalTransactionId = processResult.externalId;
                payout.UpdatedAt = now;

                _logger.LogInformation(
                    "Payout {PayoutNumber} completed successfully. External ID: {ExternalId}",
                    payout.PayoutNumber,
                    processResult.externalId);

                result.Success = true;

                // Send email notification to seller after payout is completed
                try
                {
                    // Reload payout with full navigation properties for email
                    var payoutWithDetails = await _context.Payouts
                        .Include(p => p.Store)
                            .ThenInclude(s => s.User)
                        .Include(p => p.PayoutMethod)
                        .FirstOrDefaultAsync(p => p.Id == payout.Id);

                    if (payoutWithDetails != null)
                    {
                        await _emailService.SendPayoutNotificationToSellerAsync(payoutWithDetails);
                        
                        // Create notification for seller
                        await _notificationService.CreateNotificationAsync(
                            payoutWithDetails.Store.UserId,
                            NotificationType.PayoutCompleted,
                            "Payout Completed",
                            $"Payout #{payoutWithDetails.PayoutNumber} of ${payoutWithDetails.Amount:N2} has been completed.",
                            $"/Seller/Payouts/Details/{payoutWithDetails.Id}",
                            payoutWithDetails.Id,
                            "Payout");
                    }
                }
                catch (Exception ex)
                {
                    // Don't fail the payout if email notification fails
                    _logger.LogError(ex, "Failed to send seller notification for payout {PayoutNumber}", payout.PayoutNumber);
                }
            }
            else
            {
                // Get retry delay from configuration
                var retryDelayHours = _configuration.GetValue<int>("Payout:RetryDelayHours", 24);

                // Mark as failed
                payout.Status = PayoutStatus.Failed;
                payout.FailedAt = now;
                payout.ErrorMessage = processResult.errorMessage;
                payout.ErrorReference = processResult.errorReference;
                payout.RetryCount++;
                payout.UpdatedAt = now;

                // Schedule retry if under max attempts
                if (payout.RetryCount < payout.MaxRetryAttempts)
                {
                    payout.NextRetryDate = now.AddHours(retryDelayHours * payout.RetryCount);
                    _logger.LogWarning(
                        "Payout {PayoutNumber} failed (attempt {RetryCount}/{MaxRetries}). Will retry at {NextRetry}. Error: {Error}",
                        payout.PayoutNumber,
                        payout.RetryCount,
                        payout.MaxRetryAttempts,
                        payout.NextRetryDate,
                        processResult.errorMessage);
                }
                else
                {
                    _logger.LogError(
                        "Payout {PayoutNumber} failed permanently after {RetryCount} attempts. Error: {Error}",
                        payout.PayoutNumber,
                        payout.RetryCount,
                        processResult.errorMessage);
                }

                result.Errors.Add(processResult.errorMessage ?? "Payout processing failed.");
            }

            await _context.SaveChangesAsync();
            result.Payout = payout;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error processing payout {PayoutNumber}",
                payout.PayoutNumber);

            // Revert to scheduled if processing fails
            payout.Status = PayoutStatus.Scheduled;
            payout.UpdatedAt = now;
            await _context.SaveChangesAsync();

            result.Errors.Add($"Unexpected error: {ex.Message}");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<int> RetryFailedPayoutsAsync()
    {
        var now = DateTime.UtcNow;

        // Find failed payouts that are due for retry
        var failedPayouts = await _context.Payouts
            .Where(p => p.Status == PayoutStatus.Failed
                && p.RetryCount < p.MaxRetryAttempts
                && p.NextRetryDate <= now)
            .ToListAsync();

        var retriedCount = 0;

        foreach (var payout in failedPayouts)
        {
            try
            {
                // Change status back to scheduled to allow reprocessing
                payout.Status = PayoutStatus.Scheduled;
                payout.UpdatedAt = now;
                await _context.SaveChangesAsync();

                var result = await ProcessPayoutAsync(payout.Id);
                if (result.Success)
                {
                    retriedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error retrying failed payout {PayoutId}",
                    payout.Id);
            }
        }

        _logger.LogInformation(
            "Retried {RetriedCount} failed payouts from {TotalPayouts} eligible for retry",
            retriedCount,
            failedPayouts.Count);

        return retriedCount;
    }

    /// <inheritdoc />
    public async Task<List<Payout>> GetPayoutsAsync(int storeId, PayoutStatus? status = null)
    {
        var query = _context.Payouts
            .Where(p => p.StoreId == storeId)
            .Include(p => p.PayoutMethod)
            .Include(p => p.PayoutSchedule)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Payout?> GetPayoutAsync(int payoutId)
    {
        return await _context.Payouts
            .Include(p => p.PayoutMethod)
            .Include(p => p.PayoutSchedule)
            .Include(p => p.EscrowTransactions)
            .FirstOrDefaultAsync(p => p.Id == payoutId);
    }

    /// <summary>
    /// Calculates the next payout date based on frequency and current date.
    /// </summary>
    private static DateTime CalculateNextPayoutDate(
        PayoutFrequency frequency,
        int? dayOfWeek,
        int? dayOfMonth,
        DateTime fromDate)
    {
        var currentDate = fromDate.Date;

        return frequency switch
        {
            PayoutFrequency.Weekly => CalculateNextWeeklyDate(currentDate, dayOfWeek!.Value),
            PayoutFrequency.BiWeekly => CalculateNextBiWeeklyDate(currentDate, dayOfWeek!.Value),
            PayoutFrequency.Monthly => CalculateNextMonthlyDate(currentDate, dayOfMonth!.Value),
            _ => throw new ArgumentException($"Unknown payout frequency: {frequency}")
        };
    }

    /// <summary>
    /// Calculates the next weekly payout date.
    /// </summary>
    private static DateTime CalculateNextWeeklyDate(DateTime currentDate, int targetDayOfWeek)
    {
        var daysUntilTarget = ((int)targetDayOfWeek - (int)currentDate.DayOfWeek + 7) % 7;
        if (daysUntilTarget == 0)
        {
            daysUntilTarget = 7; // Move to next week if today is the target day
        }
        return currentDate.AddDays(daysUntilTarget);
    }

    /// <summary>
    /// Calculates the next bi-weekly payout date.
    /// </summary>
    private static DateTime CalculateNextBiWeeklyDate(DateTime currentDate, int targetDayOfWeek)
    {
        var nextWeekly = CalculateNextWeeklyDate(currentDate, targetDayOfWeek);
        return nextWeekly.AddDays(7); // Add another week for bi-weekly
    }

    /// <summary>
    /// Calculates the next monthly payout date.
    /// </summary>
    private static DateTime CalculateNextMonthlyDate(DateTime currentDate, int targetDayOfMonth)
    {
        var year = currentDate.Year;
        var month = currentDate.Month;
        var day = Math.Min(targetDayOfMonth, DateTime.DaysInMonth(year, month));

        var targetDate = new DateTime(year, month, day);

        if (targetDate <= currentDate)
        {
            // Move to next month
            month++;
            if (month > 12)
            {
                month = 1;
                year++;
            }
            day = Math.Min(targetDayOfMonth, DateTime.DaysInMonth(year, month));
            targetDate = new DateTime(year, month, day);
        }

        return targetDate;
    }

    /// <summary>
    /// Generates a unique payout number.
    /// </summary>
    private static string GeneratePayoutNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"PO-{timestamp}-{random}";
    }

    /// <summary>
    /// Simulates payout processing with a payment provider.
    /// In a real implementation, this would integrate with Stripe, PayPal, or a banking API.
    /// </summary>
    private async Task<(bool success, string? externalId, string? errorMessage, string? errorReference)> SimulatePayoutProcessingAsync(Payout payout)
    {
        // Simulate processing delay
        await Task.Delay(100);

        // Simulate 90% success rate for demonstration
        var isSuccess = Random.Shared.Next(100) < 90;

        if (isSuccess)
        {
            var externalId = $"EXT-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            return (true, externalId, null, null);
        }
        else
        {
            var errorRef = $"ERR-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
            var errorMessage = "Payment provider error: Insufficient funds or account verification required.";
            return (false, null, errorMessage, errorRef);
        }
    }
}
