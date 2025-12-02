# Review Moderation Feature - Implementation Summary

## Overview
This implementation adds a comprehensive review moderation system to the MercatoApp marketplace, allowing admins to manage and moderate product reviews to ensure content quality and policy compliance.

## Epic
Reviews, Ratings & Reputation

## User Story
As an admin I want to moderate reviews so that inappropriate or fraudulent content is removed.

## Acceptance Criteria Met
✅ **Given a new review is submitted, when flagged by automated rules or manually reported, then admin can approve, reject or edit visibility.**
- Automated flagging system detects inappropriate content, spam, personal information, and policy violations
- Users can manually report reviews via the flag/report interface
- Admins can approve, reject, or toggle visibility of reviews through the admin dashboard

✅ **Given a review is rejected, when removed, then it no longer appears publicly.**
- Rejected reviews have their visibility flag set to false
- Public product pages only display approved reviews
- Review moderation status is tracked separately from visibility for flexibility

✅ **Moderation may include keyword search, automated filters and manual actions.**
- Automated keyword detection for inappropriate language
- URL and email detection to prevent spam and personal information exposure
- Phone number pattern detection
- Excessive caps detection for spam prevention
- Manual flag/report functionality for user-initiated reports

✅ **Audit log is required.**
- Complete audit trail via ReviewModerationLog model
- All moderation actions are logged with timestamp, admin user, action type, and reason
- Historical view of all actions taken on each review
- Previous and new status tracking for transparency

## Components Implemented

### 1. Data Models

#### ReviewModerationStatus (Enum)
- `PendingReview`: Review awaiting moderation
- `Approved`: Review approved and publicly visible
- `Rejected`: Review rejected and hidden from public view
- `Flagged`: Review has active flags but may still be visible

#### ReviewFlagReason (Enum)
- `InappropriateLanguage`: Contains profanity or offensive content
- `Spam`: Appears to be spam or fake
- `PersonalInformation`: Contains email, phone, or other personal info
- `OffTopic`: Not relevant to the product
- `Harassment`: Contains threats or harassment
- `Fraudulent`: Appears fraudulent or fake
- `UserReported`: Manually reported by a user
- `Other`: Other reasons

#### ReviewModerationAction (Enum)
- `Approved`: Admin approved the review
- `Rejected`: Admin rejected the review
- `Flagged`: Review was flagged for review
- `Unflagged`: Flag was removed
- `VisibilityEdited`: Visibility was changed
- `AutoApproved`: System auto-approved
- `AutoFlagged`: System auto-flagged

#### ReviewFlag Model
Represents flags raised on reviews for moderation:
- Links to ProductReview and flagging user
- Tracks reason, details, and automated vs. manual flags
- Includes resolution status and resolving admin

#### ReviewModerationLog Model
Audit log entry for moderation actions:
- Links to ProductReview and moderating admin
- Records action type, reason, and status changes
- Timestamps all actions for accountability

#### ProductReview Model Extensions
Extended with:
- `ModerationStatus`: Current moderation state
- `ModeratedByUserId`: Last admin who moderated
- `ModeratedAt`: Timestamp of last moderation
- `Flags`: Collection of flags on the review
- `ModerationLogs`: Audit trail of moderation actions

### 2. Service Layer

#### IReviewModerationService
Complete interface for review moderation operations:
- `FlagReviewAsync()`: Flag a review for moderation
- `ApproveReviewAsync()`: Approve and make visible
- `RejectReviewAsync()`: Reject and hide from public
- `ToggleReviewVisibilityAsync()`: Change visibility independently
- `ResolveFlagAsync()`: Mark a flag as resolved
- `GetFlaggedReviewsAsync()`: Retrieve all flagged reviews
- `GetReviewsByStatusAsync()`: Get reviews by moderation status
- `GetReviewModerationHistoryAsync()`: Get audit trail for a review
- `AutoCheckReviewAsync()`: Automated content checking
- `GetModerationStatsAsync()`: Statistics for dashboard
- `GetReviewByIdAsync()`: Efficient single review lookup
- `GetFlagsByReviewIdAsync()`: Get flags for a specific review

#### ReviewModerationService Implementation
Features:
- **Automated Content Filtering**:
  - Keyword detection for inappropriate language (configurable list)
  - URL pattern detection to prevent spam
  - Email address detection
  - Phone number pattern detection
  - Excessive capitalization detection (>70% caps)
  
- **Flag Management**:
  - Prevents duplicate active flags
  - Auto-resolves flags when review is approved/rejected
  - Tracks automated vs. manual flags separately
  
- **Audit Logging**:
  - Every moderation action is logged automatically
  - Captures previous and new status for transparency
  - Records moderating user and timestamp
  
- **Status Management**:
  - Handles transitions between all moderation states
  - Updates review visibility based on status
  - Manages flag lifecycle

### 3. Admin User Interface

#### Admin Review Moderation Dashboard (`/Admin/Reviews`)
Features:
- **Statistics Cards**: Shows counts for active flags, pending, approved, and rejected reviews
- **Tabbed Interface**:
  - Flagged Reviews: Shows all reviews with active flags
  - Pending Reviews: Reviews awaiting initial moderation
  - Approved Reviews: Currently approved reviews
  - Rejected Reviews: Reviews that have been rejected
  
- **Review Cards Display**:
  - Product information and review content
  - Rating visualization (star display)
  - Reviewer information (anonymized)
  - Flag details (reason, date, automated vs. manual)
  - Moderation status badges
  
- **Actions Available**:
  - Approve Review: Approve and make publicly visible
  - Reject Review: Reject with required reason
  - Toggle Visibility: Show/hide independently of status
  - Resolve Flag: Mark flag as handled without changing review status
  - View Details: Navigate to detailed review page

#### Review Details Page (`/Admin/Reviews/Details`)
Comprehensive view of a single review:
- **Review Content Section**: Full review text, rating, product info, reviewer details
- **Flags Section**: All flags (active and resolved) with details and resolution info
- **Moderation History**: Complete audit trail showing:
  - All actions taken
  - Admin who took each action
  - Status transitions
  - Timestamps and reasons
  
- **Status & Actions Panel**:
  - Current moderation status
  - Visibility status
  - Last moderator information
  - Quick action buttons for approve/reject

### 4. User-Facing Features

#### Report Review Functionality
On the product page (`/Product`):
- "Report" button appears next to each review for authenticated users
- Modal dialog for submitting reports with:
  - Dropdown to select flag reason
  - Optional text field for additional details
  - Real-time submission via API
  - Success/error feedback

#### JavaScript API Integration
- `submitFlag()` function handles client-side flag submission
- AJAX call to `/Api/FlagReview` endpoint
- JSON request with review ID, reason, and details
- User-friendly success/error messages

### 5. API Endpoints

#### FlagReview API (`/Api/FlagReview`)
POST endpoint for user-initiated flags:
- Requires authentication
- Validates flag reason enum
- Creates ReviewFlag record with user ID
- Returns JSON success/error response
- Logs all submissions for audit

### 6. Integration Points

#### ProductReviewService
Enhanced to:
- Set initial moderation status on new reviews (`Approved` by default)
- Maintain backward compatibility with existing review system

#### OrderDetailModel
Updated to:
- Inject IReviewModerationService
- Auto-check reviews after submission
- Gracefully handles auto-check failures (logs but doesn't block submission)

## Automated Flagging Rules

The system automatically flags reviews that:
1. **Contain inappropriate keywords**: "spam", "fake", "scam", "fraud", "cheat", "lie", "liar", "steal"
2. **Include URLs**: http, www, .com, .org, .net, .io references
3. **Contain email addresses**: Regex pattern match for email format
4. **Include phone numbers**: Detects US phone number patterns (xxx-xxx-xxxx)
5. **Use excessive capitals**: More than 70% of letters capitalized (spam indicator)

All automated flags are marked with `IsAutomated = true` for admin visibility.

## Security Considerations

✅ **Authorization**: All admin moderation pages require `AdminOnly` policy
✅ **Authentication**: Manual flag API requires user authentication
✅ **Input Validation**: All user inputs are validated and sanitized
✅ **CSRF Protection**: Anti-forgery tokens on all forms and API calls
✅ **Audit Trail**: Complete logging of all moderation actions
✅ **No SQL Injection**: Uses parameterized queries via EF Core
✅ **No Secrets Exposed**: No credentials or sensitive data in code
✅ **CodeQL Clean**: Zero security vulnerabilities detected

## Performance Optimizations

1. **Efficient Database Queries**:
   - `GetReviewByIdAsync()` for direct lookups instead of loading all reviews
   - `GetFlagsByReviewIdAsync()` for targeted flag retrieval
   - Proper use of Include() for eager loading related entities
   - Pagination support for large result sets

2. **Query Optimization**:
   - Indexes recommended on: ProductReviewId, ModerationStatus, UserId
   - Filtered queries prevent loading unnecessary data
   - Status-based filtering at database level

3. **Regex Efficiency**:
   - Removed redundant case conversions
   - Optimized pattern matching
   - Early exit on first match for automated checks

## Testing Considerations

### Manual Testing Scenarios
1. **Admin Moderation Dashboard**:
   - Verify all tabs load correctly
   - Check statistics are accurate
   - Test approve/reject/visibility actions
   - Verify flag resolution

2. **Automated Flagging**:
   - Submit reviews with inappropriate keywords
   - Test URL detection
   - Verify email/phone detection
   - Check caps detection threshold

3. **User Reporting**:
   - Test flag submission from product page
   - Verify modal functionality
   - Check API response handling
   - Ensure only authenticated users can report

4. **Audit Trail**:
   - Verify all actions are logged
   - Check timestamp accuracy
   - Validate status transition tracking

### Integration Testing
- Review submission triggers auto-check
- Flagged reviews appear in admin dashboard
- Rejected reviews don't appear on product pages
- Approved reviews are publicly visible
- Flag resolution updates review status

## Database Migrations

When migrating to a persistent database:
```bash
dotnet ef migrations add AddReviewModeration
dotnet ef database update
```

## Future Enhancement Opportunities

1. **Advanced Filtering**:
   - Machine learning-based content analysis
   - Sentiment analysis
   - Multi-language support for keyword detection

2. **Admin Features**:
   - Bulk approval/rejection
   - Custom keyword lists per category
   - Automated action rules (e.g., auto-reject on X flags)
   - Review edit capabilities

3. **User Features**:
   - Appeal rejected reviews
   - View own flag history
   - Notification when flag is resolved

4. **Analytics**:
   - Moderation trends dashboard
   - Flag reason analytics
   - Admin performance metrics
   - Review quality scores

5. **Workflow**:
   - Multi-level moderation (reviewer → supervisor)
   - Escalation paths for complex cases
   - SLA tracking for moderation response time

## Build Status
✅ Build successful with no errors
✅ No security vulnerabilities detected by CodeQL
✅ All code review feedback addressed

## Summary

This implementation fully addresses the acceptance criteria for review moderation:
- ✅ Automated flagging rules implemented
- ✅ Manual reporting functionality available
- ✅ Admin approve/reject/edit visibility capabilities
- ✅ Rejected reviews hidden from public view
- ✅ Complete audit logging
- ✅ Moderation includes keyword search and automated filters
- ✅ Manual admin actions supported
- ✅ No security vulnerabilities
- ✅ Performance optimized
- ✅ Production-ready code quality
