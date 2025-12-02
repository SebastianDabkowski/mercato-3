# Return/Complaint Request Feature - Implementation Overview

## Feature Summary
Buyers can now submit return or complaint requests for delivered orders directly from the order details page. The system supports two types of requests:
- **Returns**: Standard return requests for unwanted items
- **Complaints**: Product issue reports for damaged/defective/incorrect items

## Key Components

### Models
- **ReturnRequestType** (new): Enum distinguishing between Return and Complaint
- **ReturnRequest**: Updated with `RequestType` field
- Case number format: `RTN-YYYYMMDD-XXXXXXXX` for returns, `CMP-YYYYMMDD-XXXXXXXX` for complaints

### Pages
- **OrderDetail.cshtml**: Enhanced with modal form for submitting return/complaint requests
- **ReturnRequests.cshtml** (new): Dedicated page listing all buyer's cases
- Form includes: request type, reason dropdown, and optional description (max 1000 chars)

### Service Layer
- `IReturnRequestService.CreateReturnRequestAsync()`: Updated signature to include `ReturnRequestType`
- `ReturnRequestService`: Generates unique case IDs with appropriate prefix based on type
- Existing validation logic prevents duplicate requests per sub-order

### UI/UX Improvements
- Status badge displays "Pending Seller Review" instead of "Requested"
- Navigation menu includes "Returns & Complaints" link
- Client-side validation using Bootstrap 5
- Server-side validation with explicit enum checks
- Icons differentiate between returns (arrow-return-left) and complaints (exclamation-triangle)

## Validation & Security

### Client-Side
- Bootstrap form validation for required fields
- Maximum length enforcement (1000 chars for description)
- Dropdown validation for request type and reason

### Server-Side
- Explicit enum validation (not using `Enum.IsDefined()` to prevent bypass)
- Description length validation
- Authorization check (buyer must own the order)
- Eligibility validation (order must be delivered, within return window, no duplicate requests)

### Security Scan Results
- ✅ CodeQL: 0 vulnerabilities found
- ✅ CSRF protection via anti-forgery tokens
- ✅ Proper authorization checks

## Testing
- Test scenario created: `ReturnComplaintTestScenario.cs`
- Validates request creation for both return and complaint types
- Tests list retrieval functionality
- Runs automatically on application startup in development

## Usage Flow

1. Buyer navigates to order details page
2. For delivered sub-orders, "Request Return or Report Issue" button appears
3. Clicking opens modal with form:
   - Select request type (Return/Complaint)
   - Select reason from dropdown
   - Optionally add description
4. On submit, creates request with unique case ID
5. Request appears on order detail page and in "Returns & Complaints" list
6. Status shown as "Pending Seller Review"

## Future Enhancements (Out of Scope)
- Partial return (specific items, not full sub-order)
- Photo upload for complaints
- Seller response interface
- Automatic refund processing on approval
- Item-level duplicate prevention (currently at sub-order level)
