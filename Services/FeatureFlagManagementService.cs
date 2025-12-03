using System.Text.Json;
using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Implementation of feature flag management service.
/// </summary>
public class FeatureFlagManagementService : IFeatureFlagManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FeatureFlagManagementService> _logger;

    public FeatureFlagManagementService(ApplicationDbContext context, ILogger<FeatureFlagManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<FeatureFlag>> GetAllFlagsAsync(bool activeOnly = false)
    {
        var query = _context.FeatureFlags
            .Include(f => f.Rules.OrderBy(r => r.Priority))
            .Include(f => f.CreatedByUser)
            .Include(f => f.UpdatedByUser)
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(f => f.IsActive);
        }

        return await query.OrderBy(f => f.Name).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<FeatureFlag?> GetFlagByIdAsync(int id)
    {
        return await _context.FeatureFlags
            .Include(f => f.Rules.OrderBy(r => r.Priority))
            .Include(f => f.CreatedByUser)
            .Include(f => f.UpdatedByUser)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    /// <inheritdoc />
    public async Task<FeatureFlag?> GetFlagByKeyAsync(string key)
    {
        return await _context.FeatureFlags
            .Include(f => f.Rules.OrderBy(r => r.Priority))
            .FirstOrDefaultAsync(f => f.Key == key);
    }

    /// <inheritdoc />
    public async Task<FeatureFlag> CreateFlagAsync(FeatureFlag flag, int createdByUserId, string? ipAddress, string? userAgent)
    {
        flag.CreatedByUserId = createdByUserId;
        flag.CreatedAt = DateTime.UtcNow;

        _context.FeatureFlags.Add(flag);
        await _context.SaveChangesAsync();

        // Log the creation in history
        await LogHistoryAsync(flag.Id, FeatureFlagChangeType.Created, null, SerializeFlag(flag), 
            $"Feature flag '{flag.Name}' created", createdByUserId, ipAddress, userAgent);

        _logger.LogInformation("Feature flag '{FlagKey}' created by user {UserId}", flag.Key, createdByUserId);

        return flag;
    }

    /// <inheritdoc />
    public async Task<FeatureFlag> UpdateFlagAsync(FeatureFlag flag, int updatedByUserId, string? ipAddress, string? userAgent)
    {
        var existingFlag = await GetFlagByIdAsync(flag.Id);
        if (existingFlag == null)
        {
            throw new InvalidOperationException($"Feature flag with ID {flag.Id} not found");
        }

        var previousState = SerializeFlag(existingFlag);

        // Update the flag
        existingFlag.Name = flag.Name;
        existingFlag.Description = flag.Description;
        existingFlag.IsEnabledByDefault = flag.IsEnabledByDefault;
        existingFlag.IsActive = flag.IsActive;
        existingFlag.Environments = flag.Environments;
        existingFlag.UpdatedByUserId = updatedByUserId;
        existingFlag.UpdatedAt = DateTime.UtcNow;

        // Update rules - remove old ones and add new ones
        _context.FeatureFlagRules.RemoveRange(existingFlag.Rules);
        existingFlag.Rules = flag.Rules;

        await _context.SaveChangesAsync();

        // Log the update in history
        await LogHistoryAsync(flag.Id, FeatureFlagChangeType.Updated, previousState, SerializeFlag(existingFlag),
            $"Feature flag '{flag.Name}' updated", updatedByUserId, ipAddress, userAgent);

        _logger.LogInformation("Feature flag '{FlagKey}' updated by user {UserId}", flag.Key, updatedByUserId);

        return existingFlag;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFlagAsync(int id, int deletedByUserId, string? ipAddress, string? userAgent)
    {
        var flag = await GetFlagByIdAsync(id);
        if (flag == null)
        {
            return false;
        }

        var previousState = SerializeFlag(flag);

        // Log the deletion in history before removing
        await LogHistoryAsync(id, FeatureFlagChangeType.Deleted, previousState, null,
            $"Feature flag '{flag.Name}' deleted", deletedByUserId, ipAddress, userAgent);

        _context.FeatureFlags.Remove(flag);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Feature flag '{FlagKey}' deleted by user {UserId}", flag.Key, deletedByUserId);

        return true;
    }

    /// <inheritdoc />
    public async Task<FeatureFlag?> ToggleFlagAsync(int id, bool isEnabled, int updatedByUserId, string? ipAddress, string? userAgent)
    {
        var flag = await GetFlagByIdAsync(id);
        if (flag == null)
        {
            return null;
        }

        var previousState = SerializeFlag(flag);

        flag.IsEnabledByDefault = isEnabled;
        flag.UpdatedByUserId = updatedByUserId;
        flag.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Log the toggle in history
        await LogHistoryAsync(id, FeatureFlagChangeType.Toggled, previousState, SerializeFlag(flag),
            $"Feature flag '{flag.Name}' toggled to {(isEnabled ? "enabled" : "disabled")}", 
            updatedByUserId, ipAddress, userAgent);

        _logger.LogInformation("Feature flag '{FlagKey}' toggled to {IsEnabled} by user {UserId}", 
            flag.Key, isEnabled, updatedByUserId);

        return flag;
    }

    /// <inheritdoc />
    public async Task<List<FeatureFlagHistory>> GetFlagHistoryAsync(int featureFlagId)
    {
        return await _context.FeatureFlagHistories
            .Include(h => h.ChangedByUser)
            .Where(h => h.FeatureFlagId == featureFlagId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();
    }

    private async Task LogHistoryAsync(int featureFlagId, FeatureFlagChangeType changeType, 
        string? previousState, string? newState, string description, 
        int changedByUserId, string? ipAddress, string? userAgent)
    {
        var history = new FeatureFlagHistory
        {
            FeatureFlagId = featureFlagId,
            ChangeType = changeType,
            PreviousState = previousState,
            NewState = newState,
            ChangeDescription = description,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        _context.FeatureFlagHistories.Add(history);
        await _context.SaveChangesAsync();
    }

    private string SerializeFlag(FeatureFlag flag)
    {
        return JsonSerializer.Serialize(new
        {
            flag.Key,
            flag.Name,
            flag.Description,
            flag.IsEnabledByDefault,
            flag.IsActive,
            flag.Environments,
            Rules = flag.Rules.Select(r => new
            {
                r.Priority,
                r.RuleType,
                r.RuleValue,
                r.IsEnabled,
                r.Description
            })
        });
    }
}
