using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Services;

/// <summary>
/// Service for managing security incidents.
/// Supports incident creation, status tracking, alerting, and compliance reporting.
/// </summary>
public class SecurityIncidentService : ISecurityIncidentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SecurityIncidentService> _logger;
    private readonly IConfiguration _configuration;

    public SecurityIncidentService(
        ApplicationDbContext context,
        ILogger<SecurityIncidentService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<SecurityIncident> CreateIncidentAsync(CreateSecurityIncidentData data)
    {
        // Generate unique incident number
        var incidentNumber = await GenerateIncidentNumberAsync();

        var incident = new SecurityIncident
        {
            IncidentNumber = incidentNumber,
            IncidentType = data.IncidentType,
            Severity = data.Severity,
            Status = SecurityIncidentStatus.New,
            DetectionRule = data.DetectionRule,
            Source = data.Source,
            UserId = data.UserId,
            Details = data.Details,
            Metadata = data.Metadata,
            DetectedAt = data.DetectedAt ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.SecurityIncidents.Add(incident);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Security incident created: {IncidentNumber}, Type={Type}, Severity={Severity}, Source={Source}",
            incident.IncidentNumber,
            incident.IncidentType,
            incident.Severity,
            incident.Source);

        // Send alert for high-severity incidents
        if (ShouldSendAlert(incident.Severity))
        {
            await SendIncidentAlertAsync(incident);
        }

        return incident;
    }

    /// <inheritdoc />
    public async Task<SecurityIncident> UpdateIncidentStatusAsync(
        int incidentId,
        UpdateSecurityIncidentStatusData data)
    {
        var incident = await _context.SecurityIncidents
            .FirstOrDefaultAsync(i => i.Id == incidentId);

        if (incident == null)
        {
            throw new InvalidOperationException($"Security incident with ID {incidentId} not found.");
        }

        var previousStatus = incident.Status;

        // Create status history entry
        var statusHistory = new SecurityIncidentStatusHistory
        {
            SecurityIncidentId = incident.Id,
            PreviousStatus = previousStatus,
            NewStatus = data.NewStatus,
            ChangedByUserId = data.UpdatedByUserId,
            Notes = data.Notes,
            ChangedAt = DateTime.UtcNow
        };

        _context.SecurityIncidentStatusHistories.Add(statusHistory);

        // Update incident
        incident.Status = data.NewStatus;
        incident.UpdatedByUserId = data.UpdatedByUserId;
        incident.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(data.ResolutionNotes))
        {
            incident.ResolutionNotes = data.ResolutionNotes;
        }

        if (data.NewStatus == SecurityIncidentStatus.Resolved ||
            data.NewStatus == SecurityIncidentStatus.FalsePositive ||
            data.NewStatus == SecurityIncidentStatus.Closed)
        {
            incident.ResolvedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Security incident status updated: {IncidentNumber}, {PreviousStatus} -> {NewStatus}, UpdatedBy={UserId}",
            incident.IncidentNumber,
            previousStatus,
            data.NewStatus,
            data.UpdatedByUserId);

        return incident;
    }

    /// <inheritdoc />
    public async Task<SecurityIncident?> GetIncidentByIdAsync(int incidentId)
    {
        return await _context.SecurityIncidents
            .Include(i => i.User)
            .Include(i => i.UpdatedByUser)
            .FirstOrDefaultAsync(i => i.Id == incidentId);
    }

    /// <inheritdoc />
    public async Task<SecurityIncident?> GetIncidentByNumberAsync(string incidentNumber)
    {
        return await _context.SecurityIncidents
            .Include(i => i.User)
            .Include(i => i.UpdatedByUser)
            .FirstOrDefaultAsync(i => i.IncidentNumber == incidentNumber);
    }

    /// <inheritdoc />
    public async Task<PaginatedList<SecurityIncident>> GetIncidentsAsync(
        SecurityIncidentFilter? filter = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.SecurityIncidents
            .Include(i => i.User)
            .Include(i => i.UpdatedByUser)
            .AsQueryable();

        // Apply filters
        if (filter != null)
        {
            if (filter.IncidentType.HasValue)
            {
                query = query.Where(i => i.IncidentType == filter.IncidentType.Value);
            }

            if (filter.Severity.HasValue)
            {
                query = query.Where(i => i.Severity == filter.Severity.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(i => i.Status == filter.Status.Value);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(i => i.DetectedAt >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(i => i.DetectedAt <= filter.EndDate.Value);
            }

            if (filter.UserId.HasValue)
            {
                query = query.Where(i => i.UserId == filter.UserId.Value);
            }
        }

        // Order by detected date descending (most recent first)
        query = query.OrderByDescending(i => i.DetectedAt);

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedList<SecurityIncident>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc />
    public async Task<List<SecurityIncidentStatusHistory>> GetIncidentStatusHistoryAsync(int incidentId)
    {
        return await _context.SecurityIncidentStatusHistories
            .Where(h => h.SecurityIncidentId == incidentId)
            .Include(h => h.ChangedByUser)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> SendIncidentAlertAsync(SecurityIncident incident)
    {
        try
        {
            // Get security contact emails from configuration
            var securityContacts = _configuration
                .GetSection("Security:AlertRecipients")
                .Get<string[]>() ?? Array.Empty<string>();

            if (securityContacts.Length == 0)
            {
                _logger.LogWarning(
                    "No security contacts configured. Skipping alert for incident {IncidentNumber}",
                    incident.IncidentNumber);
                return false;
            }

            // Log alert instead of sending actual email in development/demo
            // In production, this would integrate with email service or incident management tools
            _logger.LogWarning(
                "Security incident alert: {IncidentNumber}, Type={Type}, Severity={Severity}, " +
                "Source={Source}, Recipients={Recipients}",
                incident.IncidentNumber,
                incident.IncidentType,
                incident.Severity,
                incident.Source,
                string.Join(", ", securityContacts));

            // Update incident with alert information
            incident.AlertSent = true;
            incident.AlertSentAt = DateTime.UtcNow;
            incident.AlertRecipients = string.Join(", ", securityContacts);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Security incident alert recorded: {IncidentNumber}, Recipients={Recipients}",
                incident.IncidentNumber,
                incident.AlertRecipients);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send alert for security incident {IncidentNumber}",
                incident.IncidentNumber);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<List<SecurityIncident>> ExportIncidentsAsync(SecurityIncidentFilter filter)
    {
        var query = _context.SecurityIncidents
            .Include(i => i.User)
            .Include(i => i.UpdatedByUser)
            .AsQueryable();

        // Apply filters
        if (filter.IncidentType.HasValue)
        {
            query = query.Where(i => i.IncidentType == filter.IncidentType.Value);
        }

        if (filter.Severity.HasValue)
        {
            query = query.Where(i => i.Severity == filter.Severity.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(i => i.Status == filter.Status.Value);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(i => i.DetectedAt >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(i => i.DetectedAt <= filter.EndDate.Value);
        }

        if (filter.UserId.HasValue)
        {
            query = query.Where(i => i.UserId == filter.UserId.Value);
        }

        // Order by detected date
        query = query.OrderByDescending(i => i.DetectedAt);

        var incidents = await query.ToListAsync();

        _logger.LogInformation(
            "Security incidents exported: Count={Count}, Filter={@Filter}",
            incidents.Count,
            filter);

        return incidents;
    }

    /// <summary>
    /// Generates a unique incident number.
    /// Format: SI-YYYYMMDD-XXXXX
    /// </summary>
    private async Task<string> GenerateIncidentNumberAsync()
    {
        var today = DateTime.UtcNow.Date;
        var prefix = $"SI-{today:yyyyMMdd}-";

        // Find the highest sequence number for today
        var lastIncidentToday = await _context.SecurityIncidents
            .Where(i => i.IncidentNumber.StartsWith(prefix))
            .OrderByDescending(i => i.IncidentNumber)
            .FirstOrDefaultAsync();

        int sequence = 1;
        if (lastIncidentToday != null)
        {
            // Extract sequence number from last incident
            var lastSequence = lastIncidentToday.IncidentNumber.Substring(prefix.Length);
            if (int.TryParse(lastSequence, out var num))
            {
                sequence = num + 1;
            }
        }

        return $"{prefix}{sequence:D5}";
    }

    /// <summary>
    /// Determines if an alert should be sent based on severity.
    /// </summary>
    private bool ShouldSendAlert(SecurityIncidentSeverity severity)
    {
        // Get threshold from configuration, default to High
        var threshold = _configuration
            .GetValue<string>("Security:AlertSeverityThreshold");

        var alertThreshold = threshold switch
        {
            "Critical" => SecurityIncidentSeverity.Critical,
            "High" => SecurityIncidentSeverity.High,
            "Medium" => SecurityIncidentSeverity.Medium,
            "Low" => SecurityIncidentSeverity.Low,
            _ => SecurityIncidentSeverity.High
        };

        return severity >= alertThreshold;
    }
}
