# SLA Tracking Feature - Implementation Summary

## Overview
This feature implements Service Level Agreement (SLA) tracking for return and complaint cases, enabling admins to monitor seller responsiveness and enforce platform standards.

## Key Components

### 1. Models

#### SLAConfig
Stores SLA threshold configurations with flexible targeting:
- **CategoryId**: Optional category targeting
- **RequestType**: Optional request type (Return/Complaint) targeting
- **FirstResponseHours**: Time limit for seller's first response (default: 24h)
- **ResolutionHours**: Time limit for case resolution (default: 168h / 7 days)
- **IsActive**: Enable/disable specific configurations

Configuration hierarchy (most specific to least):
1. Category + Request Type
2. Category only
3. Request Type only
4. Default (no category or type)

#### ReturnRequest Extensions
New fields added for SLA tracking:
- `FirstResponseDeadline`: Calculated deadline for seller's first response
- `ResolutionDeadline`: Calculated deadline for case resolution
- `FirstResponseSLABreached`: Flag indicating first response SLA breach
- `ResolutionSLABreached`: Flag indicating resolution SLA breach
- `SellerFirstResponseAt`: Timestamp of seller's first action (approve, reject, or message)

### 2. Services

#### ISLAService / SLAService
Core SLA management service providing:

**Configuration Management:**
- `GetSLAConfigAsync()`: Retrieves applicable SLA config with hierarchical fallback

**Deadline Calculation:**
- `CalculateSLADeadlinesAsync()`: Calculates first response and resolution deadlines based on config

**Breach Detection:**
- `CheckAndUpdateSLABreachesAsync()`: Checks and flags SLA breaches for a specific case
- `ProcessSLABreachesAsync()`: Batch processes all pending cases for breaches

**Statistics & Reporting:**
- `GetSellerSLAStatisticsAsync()`: Gets SLA metrics for a specific seller
- `GetPlatformSLAStatisticsAsync()`: Gets aggregate platform-wide SLA metrics
- `GetAllSellerSLAStatisticsAsync()`: Gets SLA metrics for all sellers

Metrics provided:
- Total cases
- Cases resolved within SLA
- SLA compliance percentage
- First response breach count
- Resolution breach count
- Average response time (hours)
- Average resolution time (hours)

#### ReturnRequestService Updates
Enhanced to support SLA tracking:
- Calculates and sets SLA deadlines when creating cases
- Records seller's first response timestamp on approve, reject, or message

### 3. Background Services

#### SLAMonitoringService
Hosted background service that:
- Runs periodically (configurable interval, default: 30 minutes)
- Checks all pending cases for SLA breaches
- Flags cases that have exceeded deadlines
- Logs breach detection events

### 4. Admin UI

#### SLA Dashboard (`/Admin/Returns/SLADashboard`)
Comprehensive SLA reporting interface:

**Platform Statistics Card:**
- Total cases processed
- Overall SLA compliance rate
- First response breach count
- Resolution breach count

**Performance Metrics:**
- Average response time
- Average resolution time

**Per-Seller Table:**
- Store name
- Total cases
- SLA compliance rate with badge
- Breach counts with color-coded badges
- Average response and resolution times
- Sortable columns

**Filtering:**
- Date range selection (default: last 30 days)

#### Enhanced Admin Returns Index
Updated with SLA monitoring features:

**Summary Cards:**
- Response SLA Breach count (red badge)
- Resolution SLA Breach count (red badge)

**Table Columns:**
- SLA Status column showing:
  - Resolution breach (red badge with warning icon)
  - First response breach (yellow badge with alert icon)
  - Approaching deadline (yellow badge with countdown)
  - On track (green badge with checkmark)

**Visual Indicators:**
- Row highlighting for breached cases (red background)
- Tooltip showing exact deadline times

### 5. Configuration

New settings in `appsettings.json`:

```json
{
  "SLA": {
    "CheckIntervalMinutes": 30,
    "DefaultFirstResponseHours": 24,
    "DefaultResolutionHours": 168
  }
}
```

- **CheckIntervalMinutes**: How often the background service checks for breaches
- **DefaultFirstResponseHours**: Default first response SLA when no config exists
- **DefaultResolutionHours**: Default resolution SLA when no config exists

## Usage Flow

### Case Creation
1. Buyer creates a return/complaint request
2. System determines applicable SLA config based on category and type
3. Deadlines are calculated and stored on the case:
   - First Response Deadline = RequestedAt + FirstResponseHours
   - Resolution Deadline = RequestedAt + ResolutionHours

### Seller Response Tracking
1. Seller takes action (approve, reject, or send message)
2. If this is the first action, `SellerFirstResponseAt` is recorded
3. This timestamp is used to calculate actual response time

### Breach Detection
1. Background service runs periodically
2. For each pending case:
   - Checks if current time > FirstResponseDeadline (and no seller response yet)
   - Checks if current time > ResolutionDeadline (and not resolved/completed)
3. Sets breach flags accordingly
4. Breached cases appear highlighted in admin views

### Admin Monitoring
1. Admin views dashboard to see:
   - Platform-wide compliance metrics
   - Per-seller performance
   - Which sellers consistently meet SLAs
2. Admin views returns list to see:
   - Which cases are currently breached
   - Which cases are approaching deadlines
3. Admin can take corrective action (escalate, contact seller, etc.)

## Testing

### SLATrackingTestScenario
Comprehensive test scenario validating:
1. ✓ SLA configuration creation and retrieval
2. ✓ Deadline calculation on case creation
3. ✓ Breach detection logic
4. ✓ Statistics calculation (per-seller and platform)
5. ✓ Batch processing of all pending cases

Test runs automatically on application startup in development mode.

## Security Considerations

### Authorization
- SLA dashboard: Admin-only access (`AdminOnly` policy)
- SLA configurations: Should only be editable by admins
- SLA breach flags: System-managed, not user-editable

### Data Integrity
- Breach flags are set once and never cleared (permanent audit trail)
- Deadlines are calculated once at creation (no retroactive changes)
- Seller response timestamp recorded only on first action

### Security Scan Results
✅ CodeQL: 0 vulnerabilities detected
✅ No sensitive data exposure
✅ Proper authorization checks in place

## Future Enhancements (Not in Scope)

### Auto-Escalation
- Automatically escalate cases when resolution SLA is breached
- Send notifications to admins for SLA violations

### Custom SLA Rules
- Different SLAs for different product categories
- Different SLAs for return vs complaint
- Tiered SLAs based on order value

### Seller Notifications
- Email sellers when approaching SLA deadline
- Dashboard alerts for sellers with pending deadlines

### Performance Tracking
- Historical SLA performance trends
- Seller ranking by SLA compliance
- SLA performance as factor in seller ratings

### Advanced Statistics
- SLA performance by day of week
- SLA performance by time of day
- Correlation between SLA compliance and customer satisfaction

## Database Schema Changes

### New Table: SLAConfigs
- Id (PK)
- CategoryId (FK, nullable)
- RequestType (nullable)
- FirstResponseHours
- ResolutionHours
- IsActive
- CreatedAt
- UpdatedAt
- UpdatedByUserId (FK, nullable)

### Modified Table: ReturnRequests
Added columns:
- FirstResponseDeadline (nullable DateTime)
- ResolutionDeadline (nullable DateTime)
- FirstResponseSLABreached (bool)
- ResolutionSLABreached (bool)
- SellerFirstResponseAt (nullable DateTime)

## Performance Considerations

### Background Service
- Runs on configurable schedule (default: 30 min)
- Queries only pending cases (status = Requested, Approved, or UnderAdminReview)
- Efficient batch processing

### Dashboard
- Statistics calculated on-demand (no caching)
- Date range filter limits data volume
- Indexes recommended on:
  - ReturnRequests.RequestedAt
  - ReturnRequests.Status
  - ReturnRequests.FirstResponseSLABreached
  - ReturnRequests.ResolutionSLABreached

### Admin Index
- Real-time SLA status display
- Efficient LINQ queries
- Row highlighting based on breach flags

## Acceptance Criteria Verification

✅ **Given SLA tracking is enabled, when a new case is created, then the system records creation time and calculates SLA deadlines for first response and resolution based on configuration.**
- Implemented in `ReturnRequestService.CreateReturnRequestAsync()`
- Uses `ISLAService.CalculateSLADeadlinesAsync()`
- Deadlines stored in `FirstResponseDeadline` and `ResolutionDeadline` fields

✅ **Given a case is pending seller action, when the SLA deadline for first response or resolution is exceeded, then the system flags the case as 'SLA breached' and surfaces it in admin views.**
- Implemented in `SLAService.CheckAndUpdateSLABreachesAsync()`
- Automated by `SLAMonitoringService` background service
- Flags set in `FirstResponseSLABreached` and `ResolutionSLABreached` fields
- Surfaced in admin index with visual indicators and row highlighting

✅ **Given SLA metrics are recorded, when I open the SLA dashboard as an admin, then I can see aggregate SLA statistics (e.g. percentage of cases resolved within SLA, average response time) per seller over a selected period.**
- Implemented in `/Admin/Returns/SLADashboard`
- Shows platform-wide and per-seller statistics
- Includes compliance percentage, breach counts, and average times
- Date range filtering supported

## Technical Notes

### Why No Auto-Escalation?
Auto-escalation on resolution SLA breach was considered but deferred to avoid:
- Circular dependency between SLAService and ReturnRequestService
- Complex transaction handling
- Potential for race conditions

Instead, cases are flagged and highlighted for admin manual review.

### Why Store Deadlines?
Deadlines are calculated once at creation and stored rather than calculated on-demand because:
- SLA configs can change over time
- Cases should be measured against the SLA that was active when created
- Avoids expensive repeated calculations
- Provides audit trail

### Why Track First Response?
First response tracking is important because:
- It's a leading indicator of seller responsiveness
- Early response often leads to faster resolution
- Platform can identify sellers who are slow to engage

## Deployment Notes

### Migration Steps
1. Database will auto-create new tables/columns (in-memory DB in dev)
2. Default SLA configuration created on first test run
3. Background service starts automatically
4. No data migration needed (new feature)

### Configuration Steps
1. Review and adjust `SLA:CheckIntervalMinutes` if needed
2. Review and adjust default SLA hours if needed
3. Create custom SLA configs via admin interface (future enhancement)

### Monitoring
- Check application logs for `SLAMonitoringService` startup messages
- Monitor `SLAService` logs for breach detection events
- Review dashboard regularly for platform health

## Related Documentation
- [Return/Complaint Feature](RETURN_COMPLAINT_FEATURE.md)
- [Case Resolution Implementation](CASE_RESOLUTION_IMPLEMENTATION.md)
- [Admin Escalation Feature](ADMIN_ESCALATION_IMPLEMENTATION.md)
