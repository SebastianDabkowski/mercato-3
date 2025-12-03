using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Tests;

/// <summary>
/// Test scenario for security incident logging and management.
/// Demonstrates automatic detection, status tracking, and compliance reporting.
/// </summary>
public static class SecurityIncidentTestScenario
{
    /// <summary>
    /// Runs the test scenario for security incident management.
    /// </summary>
    public static async Task RunTestAsync(
        ApplicationDbContext context,
        ISecurityIncidentService incidentService,
        ILoginEventService loginEventService)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("Security Incident Management Test");
        Console.WriteLine("========================================");
        Console.WriteLine();

        // Test 1: Create a manual security incident
        Console.WriteLine("Test 1: Creating manual security incident");
        var manualIncident = await incidentService.CreateIncidentAsync(new CreateSecurityIncidentData
        {
            IncidentType = SecurityIncidentType.SuspiciousApiUsage,
            Severity = SecurityIncidentSeverity.High,
            DetectionRule = "Excessive API calls detected (1000+ calls in 5 minutes)",
            Source = "192.168.1.100",
            UserId = null,
            Details = "Unusual API usage pattern detected from IP 192.168.1.100",
            Metadata = "{\"callCount\":1532,\"timeWindow\":\"5 minutes\",\"endpoint\":\"/api/products\"}"
        });
        Console.WriteLine($"✓ Created incident: {manualIncident.IncidentNumber}");
        Console.WriteLine($"  Type: {manualIncident.IncidentType}");
        Console.WriteLine($"  Severity: {manualIncident.Severity}");
        Console.WriteLine($"  Status: {manualIncident.Status}");
        Console.WriteLine($"  Alert Sent: {manualIncident.AlertSent}");
        Console.WriteLine();

        // Test 2: Simulate multiple failed login attempts (should trigger automatic incident)
        Console.WriteLine("Test 2: Simulating multiple failed login attempts");
        var testUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "buyer1@example.com");
        if (testUser != null)
        {
            for (int i = 0; i < 6; i++)
            {
                await loginEventService.LogEventAsync(new LoginEventData
                {
                    UserId = testUser.Id,
                    Email = testUser.Email,
                    EventType = LoginEventType.PasswordLogin,
                    IsSuccessful = false,
                    FailureReason = "Invalid password",
                    IpAddress = "203.0.113.42",
                    UserAgent = "Mozilla/5.0 (Suspicious Browser)"
                });
            }
            Console.WriteLine($"✓ Logged 6 failed login attempts for user {testUser.Email}");
            
            // Check if incident was created
            var failedLoginIncidents = await incidentService.GetIncidentsAsync(
                new SecurityIncidentFilter
                {
                    IncidentType = SecurityIncidentType.MultipleFailedLogins,
                    UserId = testUser.Id
                });
            
            if (failedLoginIncidents.Items.Count > 0)
            {
                var incident = failedLoginIncidents.Items[0];
                Console.WriteLine($"✓ Automatic incident created: {incident.IncidentNumber}");
                Console.WriteLine($"  Detection Rule: {incident.DetectionRule}");
            }
        }
        Console.WriteLine();

        // Test 3: Update incident status
        Console.WriteLine("Test 3: Updating incident status");
        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@example.com");
        if (adminUser != null && manualIncident != null)
        {
            var updatedIncident = await incidentService.UpdateIncidentStatusAsync(
                manualIncident.Id,
                new UpdateSecurityIncidentStatusData
                {
                    NewStatus = SecurityIncidentStatus.Triaged,
                    UpdatedByUserId = adminUser.Id,
                    Notes = "Reviewed by security team. Identified as automated scraping bot. Implementing rate limiting."
                });
            
            Console.WriteLine($"✓ Updated incident {updatedIncident.IncidentNumber} to {updatedIncident.Status}");
            
            // Further update to InInvestigation
            updatedIncident = await incidentService.UpdateIncidentStatusAsync(
                manualIncident.Id,
                new UpdateSecurityIncidentStatusData
                {
                    NewStatus = SecurityIncidentStatus.InInvestigation,
                    UpdatedByUserId = adminUser.Id,
                    Notes = "Investigating source IP and implementing mitigation measures."
                });
            
            Console.WriteLine($"✓ Updated incident {updatedIncident.IncidentNumber} to {updatedIncident.Status}");
            
            // Final update to Resolved
            updatedIncident = await incidentService.UpdateIncidentStatusAsync(
                manualIncident.Id,
                new UpdateSecurityIncidentStatusData
                {
                    NewStatus = SecurityIncidentStatus.Resolved,
                    UpdatedByUserId = adminUser.Id,
                    Notes = "IP blocked and rate limiting implemented.",
                    ResolutionNotes = "Implemented rate limiting on /api/products endpoint. Blocked source IP 192.168.1.100. No data breach detected."
                });
            
            Console.WriteLine($"✓ Resolved incident {updatedIncident.IncidentNumber}");
            Console.WriteLine($"  Resolution: {updatedIncident.ResolutionNotes}");
        }
        Console.WriteLine();

        // Test 4: View incident status history
        Console.WriteLine("Test 4: Viewing incident status history");
        if (manualIncident != null)
        {
            var statusHistory = await incidentService.GetIncidentStatusHistoryAsync(manualIncident.Id);
            Console.WriteLine($"✓ Status history for {manualIncident.IncidentNumber}:");
            foreach (var history in statusHistory)
            {
                Console.WriteLine($"  {history.ChangedAt:yyyy-MM-dd HH:mm:ss} - {history.PreviousStatus} → {history.NewStatus}");
                if (!string.IsNullOrEmpty(history.Notes))
                {
                    Console.WriteLine($"    Notes: {history.Notes}");
                }
            }
        }
        Console.WriteLine();

        // Test 5: Create critical severity incident (should trigger alert)
        Console.WriteLine("Test 5: Creating critical severity incident");
        var criticalIncident = await incidentService.CreateIncidentAsync(new CreateSecurityIncidentData
        {
            IncidentType = SecurityIncidentType.SuspectedAccountCompromise,
            Severity = SecurityIncidentSeverity.Critical,
            DetectionRule = "Account access from multiple geographic locations in short time",
            Source = "45.33.32.156",
            UserId = testUser?.Id,
            Details = "User account accessed from USA and China within 5 minutes",
            Metadata = "{\"locations\":[\"USA\",\"China\"],\"timeGap\":\"5 minutes\"}"
        });
        Console.WriteLine($"✓ Created critical incident: {criticalIncident.IncidentNumber}");
        Console.WriteLine($"  Alert Sent: {criticalIncident.AlertSent}");
        if (criticalIncident.AlertSent)
        {
            Console.WriteLine($"  Alert Timestamp: {criticalIncident.AlertSentAt:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  Alert Recipients: {criticalIncident.AlertRecipients}");
        }
        Console.WriteLine();

        // Test 6: Export incidents for compliance reporting
        Console.WriteLine("Test 6: Exporting incidents for compliance");
        var exportFilter = new SecurityIncidentFilter
        {
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow
        };
        var exportedIncidents = await incidentService.ExportIncidentsAsync(exportFilter);
        
        Console.WriteLine($"✓ Exported {exportedIncidents.Count} incidents");
        Console.WriteLine("  Summary by type:");
        var byType = exportedIncidents.GroupBy(i => i.IncidentType);
        foreach (var group in byType)
        {
            Console.WriteLine($"    {group.Key}: {group.Count()}");
        }
        Console.WriteLine("  Summary by severity:");
        var bySeverity = exportedIncidents.GroupBy(i => i.Severity);
        foreach (var group in bySeverity)
        {
            Console.WriteLine($"    {group.Key}: {group.Count()}");
        }
        Console.WriteLine("  Summary by status:");
        var byStatus = exportedIncidents.GroupBy(i => i.Status);
        foreach (var group in byStatus)
        {
            Console.WriteLine($"    {group.Key}: {group.Count()}");
        }
        Console.WriteLine();

        // Test 7: Query incidents with various filters
        Console.WriteLine("Test 7: Querying incidents with filters");
        
        var highSeverityIncidents = await incidentService.GetIncidentsAsync(
            new SecurityIncidentFilter { Severity = SecurityIncidentSeverity.High });
        Console.WriteLine($"✓ High severity incidents: {highSeverityIncidents.TotalCount}");
        
        var newIncidents = await incidentService.GetIncidentsAsync(
            new SecurityIncidentFilter { Status = SecurityIncidentStatus.New });
        Console.WriteLine($"✓ New (unreviewed) incidents: {newIncidents.TotalCount}");
        
        var resolvedIncidents = await incidentService.GetIncidentsAsync(
            new SecurityIncidentFilter { Status = SecurityIncidentStatus.Resolved });
        Console.WriteLine($"✓ Resolved incidents: {resolvedIncidents.TotalCount}");
        
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("Security Incident Test Complete");
        Console.WriteLine("========================================");
        Console.WriteLine();
    }
}
