# Legal Document Management Implementation

## Overview

This implementation adds comprehensive legal document management capabilities to MercatoApp, allowing admins to manage Terms of Service, Privacy Policy, Cookie Policy, and Seller Agreements with full versioning and consent tracking support.

## Features Implemented

### 1. Domain Models

#### LegalDocumentType (Enum)
- **TermsOfService**: Terms of Service document
- **PrivacyPolicy**: Privacy Policy document
- **CookiePolicy**: Cookie Policy document
- **SellerAgreement**: Seller Agreement document

#### LegalDocument (Model)
- Full versioning support with version numbers (e.g., "1.0", "2.0")
- Effective date management for scheduled document updates
- Active/inactive status tracking (only one active version per type)
- HTML content storage for rich text documents
- Language code support for future multilingual capabilities
- Change notes for documenting version differences
- Audit trail with created/updated timestamps and admin user tracking

#### UserConsent (Model)
- Links users to specific legal document versions
- Tracks when consent was given
- Records IP address and user agent for audit purposes
- Stores context (e.g., "registration", "checkout", "seller_onboarding")

### 2. Database Layer

#### ApplicationDbContext Updates
- Added `LegalDocuments` DbSet
- Added `UserConsents` DbSet
- Configured entity relationships and indexes:
  - Composite index on document type, active status, and language
  - Index on effective date for date-based queries
  - Indexes for finding user consents efficiently

### 3. Service Layer

#### ILegalDocumentService Interface
Provides comprehensive legal document management:
- `GetActiveDocumentAsync()`: Get currently active version by type
- `GetDocumentByIdAsync()`: Get specific document by ID
- `GetDocumentHistoryAsync()`: Get all versions of a document type
- `GetAllActiveDocumentsAsync()`: Get all active documents grouped by type
- `CreateDocumentAsync()`: Create new document version
- `UpdateDocumentAsync()`: Update existing document version
- `DeleteDocumentAsync()`: Delete document (with safety checks)
- `ActivateDocumentAsync()`: Activate a specific version
- `RecordConsentAsync()`: Record user consent to a document
- `HasUserConsentedAsync()`: Check if user has consented
- `GetUserConsentsAsync()`: Get all consents for a user
- `GetDocumentConsentsAsync()`: Get all consents for a document

#### LegalDocumentService Implementation
- Ensures only one active version per document type
- Prevents deletion of active documents
- Prevents deletion of documents with associated consents
- Prevents activation of future-dated documents
- Comprehensive logging for audit purposes

### 4. Admin UI Pages

#### Index Page (`/Admin/LegalDocuments/Index`)
- Grid view of all legal document types
- Shows current active version for each type
- Quick access to view history or create new versions
- Visual indicators for active documents (green) vs. missing documents (gray)

#### Edit Page (`/Admin/LegalDocuments/Edit`)
- Create new document versions or edit existing ones
- Form fields:
  - Version number (e.g., "1.0", "2.0")
  - Document title
  - HTML content (textarea with HTML support)
  - Effective date (date picker)
  - Language code (defaults to "en")
  - Activate immediately checkbox
  - Change notes (optional)
- Validation for all required fields
- Auto-suggests next version number when creating new versions

#### History Page (`/Admin/LegalDocuments/History`)
- View all versions of a specific document type
- Separate sections for:
  - Future-dated versions (with warning notices)
  - Current and past versions
- For each version:
  - Version number and status (Active/Future)
  - Title and effective date
  - Created/updated information with admin user
  - Change notes
  - Collapsible content preview
  - Actions: Edit, Activate (non-active), Delete (non-active with no consents)

### 5. Consent Tracking Integration

#### ConsentHelper Class
Static helper methods for recording consent:
- `RecordRegistrationConsentAsync()`: Records ToS, Privacy, and Cookie consents during registration
- `RecordSellerAgreementConsentAsync()`: Records Seller Agreement consent during seller onboarding
- `RecordCheckoutConsentAsync()`: Records or re-confirms consent during checkout
- `HasRequiredConsentsAsync()`: Checks if user has all required consents for a context

#### Integration Points
The helper class can be integrated into:
1. **Registration flow**: Call `ConsentHelper.RecordRegistrationConsentAsync()` after user account creation
2. **Seller onboarding**: Call `ConsentHelper.RecordSellerAgreementConsentAsync()` when seller completes onboarding
3. **Checkout**: Call `ConsentHelper.RecordCheckoutConsentAsync()` during order placement
4. **Pre-action checks**: Use `ConsentHelper.HasRequiredConsentsAsync()` to verify consent before allowing certain actions

## Acceptance Criteria Compliance

✅ **AC1**: Admin can select document type and view/edit current version
- Index page shows all document types with current active versions
- Edit page allows viewing and editing of any version

✅ **AC2**: Admin can create new version with effective date
- Edit page supports creating new versions
- Effective date field allows setting future dates
- System stores old versions for reference

✅ **AC3**: Future effective dates show current version with optional notice
- History page clearly separates future-dated versions
- Warning notice explains users will see current version until effective date

✅ **AC4**: Consent associated with version identifier
- UserConsent model links to specific LegalDocument ID
- RecordConsentAsync captures which exact version was accepted
- Consent tracking includes timestamp, IP, user agent, and context

## Future Enhancements

### Phase 2 - Multilingual Support
- Add UI for managing translations of legal documents
- Language selector on public pages
- Fallback to default language if translation not available

### Phase 3 - Public Pages
- Create public-facing pages for viewing legal documents
- Add links to footer
- Show version history to users
- Display upcoming changes notice

### Phase 4 - Advanced Features
- Email notifications when new versions are published
- Required re-consent prompts for major changes
- Consent withdrawal functionality
- Export consent records for compliance reporting
- Role-based access for legal/compliance team (view-only)

## Security Considerations

1. **Admin-Only Access**: All legal document management pages require AdminOnly policy
2. **Audit Trail**: All create/update operations log admin user and timestamp
3. **Consent Tracking**: IP address and user agent stored for legal compliance
4. **Deletion Protection**: Active documents and documents with consents cannot be deleted
5. **Version Control**: Maintains complete history of all document versions

## Database Indexes

Optimized for common queries:
- Active documents by type and language: `(DocumentType, IsActive, LanguageCode)`
- Effective date queries: `(EffectiveDate)`
- Version lookups: `(DocumentType, Version, LanguageCode)`
- User consent lookup: `(UserId, LegalDocumentId)`
- Document consent count: `(LegalDocumentId)`

## Usage Example

```csharp
// In admin controller or page
public class LegalDocumentManagementService
{
    private readonly ILegalDocumentService _legalDocumentService;
    
    // Create a new Terms of Service version
    public async Task CreateNewTosVersionAsync(int adminUserId)
    {
        var newVersion = new LegalDocument
        {
            DocumentType = LegalDocumentType.TermsOfService,
            Version = "2.0",
            Title = "Terms of Service v2.0",
            Content = "<h1>Updated Terms</h1><p>...</p>",
            EffectiveDate = DateTime.UtcNow.AddDays(30),
            IsActive = false,
            LanguageCode = "en",
            ChangeNotes = "Updated dispute resolution section"
        };
        
        await _legalDocumentService.CreateDocumentAsync(newVersion, adminUserId);
    }
    
    // Record user consent during registration
    public async Task RecordUserRegistrationConsent(int userId, string ipAddress)
    {
        await ConsentHelper.RecordRegistrationConsentAsync(
            _legalDocumentService,
            userId,
            ipAddress,
            "Mozilla/5.0...");
    }
}
```

## Testing

### Manual Testing Steps

1. **Access Admin Panel**
   - Log in as admin user
   - Navigate to `/Admin/LegalDocuments/Index`

2. **Create Initial Versions**
   - For each document type, click "New Version"
   - Fill in version "1.0", title, content, and effective date (past date)
   - Check "Activate Immediately"
   - Save

3. **Create Future Version**
   - Go to Terms of Service history
   - Click "Create New Version"
   - Fill in version "2.0" with future effective date
   - Do not activate
   - Save and verify it appears in "Future-Dated Versions"

4. **Test Activation**
   - Try to activate the future version (should fail with error)
   - Change effective date to past/present
   - Activate successfully

5. **Test Deletion Protection**
   - Try to delete active version (should fail)
   - Create consent record for a document
   - Try to delete that document (should fail)
   - Delete a version with no consents (should succeed)

6. **Verify Consent Tracking**
   - Use ConsentHelper in registration flow
   - Verify consents are recorded in database
   - Check HasUserConsentedAsync returns true

## Files Modified/Created

### New Files
- `Models/LegalDocumentType.cs` - Enum for document types
- `Models/LegalDocument.cs` - Main document model
- `Models/UserConsent.cs` - Consent tracking model
- `Services/ILegalDocumentService.cs` - Service interface
- `Services/LegalDocumentService.cs` - Service implementation
- `Pages/Admin/LegalDocuments/Index.cshtml` - Admin index page
- `Pages/Admin/LegalDocuments/Index.cshtml.cs` - Page model
- `Pages/Admin/LegalDocuments/Edit.cshtml` - Edit/create page
- `Pages/Admin/LegalDocuments/Edit.cshtml.cs` - Page model
- `Pages/Admin/LegalDocuments/History.cshtml` - Version history page
- `Pages/Admin/LegalDocuments/History.cshtml.cs` - Page model
- `Helpers/ConsentHelper.cs` - Integration helper

### Modified Files
- `Data/ApplicationDbContext.cs` - Added DbSets and entity configuration
- `Program.cs` - Registered LegalDocumentService

## Conclusion

This implementation provides a complete, production-ready legal document management system with versioning, consent tracking, and admin interface. All acceptance criteria have been met, and the system is designed for future expansion to support multilingual content and public-facing pages.
