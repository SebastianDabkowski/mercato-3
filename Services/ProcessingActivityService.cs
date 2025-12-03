using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing GDPR processing activities.
/// </summary>
public class ProcessingActivityService : IProcessingActivityService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProcessingActivityService> _logger;

    public ProcessingActivityService(
        ApplicationDbContext context,
        ILogger<ProcessingActivityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ProcessingActivity>> GetAllAsync(bool activeOnly = false)
    {
        try
        {
            var query = _context.ProcessingActivities
                .Include(pa => pa.CreatedByUser)
                .Include(pa => pa.UpdatedByUser)
                .AsQueryable();

            if (activeOnly)
            {
                query = query.Where(pa => pa.IsActive);
            }

            return await query
                .OrderBy(pa => pa.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving processing activities");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProcessingActivity?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.ProcessingActivities
                .Include(pa => pa.CreatedByUser)
                .Include(pa => pa.UpdatedByUser)
                .FirstOrDefaultAsync(pa => pa.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving processing activity {Id}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProcessingActivity> CreateAsync(ProcessingActivity activity, int userId)
    {
        try
        {
            activity.CreatedAt = DateTime.UtcNow;
            activity.CreatedByUserId = userId;

            _context.ProcessingActivities.Add(activity);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Created processing activity {Id} '{Name}' by user {UserId}",
                activity.Id, activity.Name, userId);

            return activity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating processing activity");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(ProcessingActivity activity, int userId, string? changeNotes = null)
    {
        try
        {
            var existing = await _context.ProcessingActivities
                .FirstOrDefaultAsync(pa => pa.Id == activity.Id);

            if (existing == null)
            {
                _logger.LogWarning("Processing activity {Id} not found for update", activity.Id);
                return false;
            }

            // Create history entry before updating
            var history = new ProcessingActivityHistory
            {
                ProcessingActivityId = existing.Id,
                Name = existing.Name,
                Purpose = existing.Purpose,
                LegalBasis = existing.LegalBasis,
                DataCategories = existing.DataCategories,
                DataSubjects = existing.DataSubjects,
                Recipients = existing.Recipients,
                InternationalTransfers = existing.InternationalTransfers,
                RetentionPeriod = existing.RetentionPeriod,
                SecurityMeasures = existing.SecurityMeasures,
                Processors = existing.Processors,
                IsActive = existing.IsActive,
                Notes = existing.Notes,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = userId,
                ChangeNotes = changeNotes
            };

            _context.ProcessingActivityHistories.Add(history);

            // Update the processing activity
            existing.Name = activity.Name;
            existing.Purpose = activity.Purpose;
            existing.LegalBasis = activity.LegalBasis;
            existing.DataCategories = activity.DataCategories;
            existing.DataSubjects = activity.DataSubjects;
            existing.Recipients = activity.Recipients;
            existing.InternationalTransfers = activity.InternationalTransfers;
            existing.RetentionPeriod = activity.RetentionPeriod;
            existing.SecurityMeasures = activity.SecurityMeasures;
            existing.Processors = activity.Processors;
            existing.IsActive = activity.IsActive;
            existing.Notes = activity.Notes;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = userId;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Updated processing activity {Id} '{Name}' by user {UserId}",
                activity.Id, activity.Name, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating processing activity {Id}", activity.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, int userId)
    {
        try
        {
            var activity = await _context.ProcessingActivities
                .FirstOrDefaultAsync(pa => pa.Id == id);

            if (activity == null)
            {
                _logger.LogWarning("Processing activity {Id} not found for deletion", id);
                return false;
            }

            // Create history entry before deleting
            var history = new ProcessingActivityHistory
            {
                ProcessingActivityId = activity.Id,
                Name = activity.Name,
                Purpose = activity.Purpose,
                LegalBasis = activity.LegalBasis,
                DataCategories = activity.DataCategories,
                DataSubjects = activity.DataSubjects,
                Recipients = activity.Recipients,
                InternationalTransfers = activity.InternationalTransfers,
                RetentionPeriod = activity.RetentionPeriod,
                SecurityMeasures = activity.SecurityMeasures,
                Processors = activity.Processors,
                IsActive = activity.IsActive,
                Notes = activity.Notes,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = userId,
                ChangeNotes = "Processing activity deactivated"
            };

            _context.ProcessingActivityHistories.Add(history);

            // Soft delete by setting IsActive to false
            activity.IsActive = false;
            activity.UpdatedAt = DateTime.UtcNow;
            activity.UpdatedByUserId = userId;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Deleted (soft) processing activity {Id} '{Name}' by user {UserId}",
                id, activity.Name, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting processing activity {Id}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<ProcessingActivityHistory>> GetHistoryAsync(int id)
    {
        try
        {
            return await _context.ProcessingActivityHistories
                .Include(h => h.ChangedByUser)
                .Where(h => h.ProcessingActivityId == id)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving history for processing activity {Id}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportToExcelAsync()
    {
        try
        {
            var activities = await GetAllAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Processing Activities");

            // Headers
            var headers = new[]
            {
                "Name",
                "Purpose",
                "Legal Basis",
                "Data Categories",
                "Data Subjects",
                "Recipients",
                "International Transfers",
                "Retention Period",
                "Security Measures",
                "Processors",
                "Status",
                "Created At",
                "Created By",
                "Updated At",
                "Updated By",
                "Notes"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            // Data
            int row = 2;
            foreach (var activity in activities)
            {
                worksheet.Cells[row, 1].Value = activity.Name;
                worksheet.Cells[row, 2].Value = activity.Purpose;
                worksheet.Cells[row, 3].Value = activity.LegalBasis;
                worksheet.Cells[row, 4].Value = activity.DataCategories;
                worksheet.Cells[row, 5].Value = activity.DataSubjects;
                worksheet.Cells[row, 6].Value = activity.Recipients;
                worksheet.Cells[row, 7].Value = activity.InternationalTransfers;
                worksheet.Cells[row, 8].Value = activity.RetentionPeriod;
                worksheet.Cells[row, 9].Value = activity.SecurityMeasures;
                worksheet.Cells[row, 10].Value = activity.Processors;
                worksheet.Cells[row, 11].Value = activity.IsActive ? "Active" : "Inactive";
                worksheet.Cells[row, 12].Value = activity.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cells[row, 13].Value = activity.CreatedByUser?.Email ?? "Unknown";
                worksheet.Cells[row, 14].Value = activity.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                worksheet.Cells[row, 15].Value = activity.UpdatedByUser?.Email ?? "";
                worksheet.Cells[row, 16].Value = activity.Notes;
                row++;
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            _logger.LogInformation("Exported {Count} processing activities to Excel", activities.Count);

            return package.GetAsByteArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting processing activities to Excel");
            throw;
        }
    }
}
