# Promo Code Feature Implementation

## Overview
This document describes the promo code feature implementation for the MercatoApp shopping cart and checkout process (Phase 2).

## Feature Status
✅ **Implemented and Ready for Testing**

The promo code feature is fully implemented but **disabled by default**. To enable it, set the feature flag in your configuration:

```json
{
  "FeatureFlags": {
    "PromoCode": true
  }
}
```

## Architecture

### Models

#### PromoCode (`Models/PromoCode.cs`)
The main model representing a promotional code with the following properties:
- **Code**: The promo code string (e.g., "SAVE20")
- **Scope**: Platform-wide or Seller-specific
- **DiscountType**: Percentage or Fixed Amount
- **DiscountValue**: The discount amount or percentage
- **MinimumOrderSubtotal**: Optional minimum order requirement
- **MaximumDiscountAmount**: Optional cap for percentage discounts
- **StartDate/ExpirationDate**: Validity period
- **MaximumUsageCount**: Optional usage limit
- **CurrentUsageCount**: Tracks how many times the code has been used
- **IsActive**: Enable/disable flag

#### PromoCodeScope (`Models/PromoCodeScope.cs`)
Enum defining the scope of a promo code:
- **Platform**: Applies to any seller's products
- **Seller**: Applies only to a specific seller's products

#### PromoCodeDiscountType (`Models/PromoCodeDiscountType.cs`)
Enum defining the type of discount:
- **Percentage**: Percentage-based discount (e.g., 20% off)
- **FixedAmount**: Fixed dollar amount discount (e.g., $10 off)

### Services

#### PromoCodeService (`Services/PromoCodeService.cs`)
Core service for promo code operations:
- **ValidatePromoCodeAsync**: Validates a promo code against various criteria (active, not expired, usage limits, etc.)
- **CalculateDiscount**: Calculates the discount amount based on promo code rules and cart contents
- **IncrementUsageCountAsync**: Increments the usage count when an order is placed

#### CartTotalsService (Updated)
Extended to support promo code discounts:
- Updated `CalculateCartTotalsAsync` to accept an optional `PromoCode` parameter
- Applies discount calculation after subtotal and shipping are computed
- Ensures total never goes negative

### Database

#### ApplicationDbContext (Updated)
- Added `DbSet<PromoCode> PromoCodes`
- Configured indexes for efficient lookups:
  - Unique index on Code
  - Index on StoreId for seller-specific codes
  - Composite index on IsActive and ExpirationDate

### UI Components

#### Cart Page (`Pages/Cart.cshtml` and `Cart.cshtml.cs`)
Enhanced with promo code functionality:
- **Input field**: Allows users to enter a promo code (only shown when feature is enabled)
- **Apply button**: Validates and applies the promo code
- **Applied code display**: Shows the currently applied code with a remove button
- **Success/Error messages**: Displays feedback to the user
- **Discount display**: Shows the discount amount in the order summary
- **Session storage**: Persists applied promo code in session

#### Checkout Review Page (`Pages/Checkout/Review.cshtml` and `Review.cshtml.cs`)
Updated to show promo code discounts:
- Retrieves applied promo code from session
- Re-validates the promo code at checkout
- Displays discount in order summary
- Includes promo code in final total calculation

### Feature Flag

The promo code feature is controlled by the `IsPromoCodeEnabled` property in `IFeatureFlagService`:
- Default: **false** (disabled)
- Configuration key: `FeatureFlags:PromoCode`

## Test Data

The following test promo codes are seeded in development:

| Code | Type | Scope | Discount | Min Order | Max Discount | Notes |
|------|------|-------|----------|-----------|--------------|-------|
| **SAVE20** | Percentage | Platform | 20% | $50 | $50 | Valid for 30 days, 100 max uses |
| **WELCOME10** | Fixed | Platform | $10 | $30 | - | Valid for 60 days |
| **ELECTRONICS15** | Percentage | Seller | 15% | $100 | $30 | Only for Test Electronics Store |
| **EXPIRED** | Percentage | Platform | 50% | - | - | Expired (for testing validation) |

## User Stories & Acceptance Criteria

### ✅ AC1: Promo Code Input Field
**Given** the promo engine is enabled  
**When** I am on the cart or checkout page  
**Then** I can enter a promo code in a dedicated input field.

**Implementation**: The cart page shows a promo code input field with an "Apply" button when `IsPromoCodeEnabled` is true.

### ✅ AC2: Valid Promo Code Application
**Given** I enter a valid promo code that applies to my cart  
**When** I click 'Apply'  
**Then** the discount is calculated, shown separately, and the order total is reduced accordingly.

**Implementation**: 
- `PromoCodeService.ValidatePromoCodeAsync` validates the code
- `PromoCodeService.CalculateDiscount` computes the discount amount
- Cart displays: Subtotal, Shipping, Discount (in green with code), and Total
- Discount respects minimum order requirements and maximum discount caps

### ✅ AC3: Invalid Promo Code Handling
**Given** I enter an invalid, expired, or ineligible promo code  
**When** I click 'Apply'  
**Then** I see a clear error message and no discount is applied.

**Implementation**:
- Validation checks: code exists, is active, not expired, within usage limits
- Minimum order validation: ensures cart meets minimum requirement
- Error messages: "Invalid, expired, or ineligible promo code" or specific minimum order message
- No discount applied on validation failure

### ✅ AC4: Single Promo Code Limitation
**Given** multiple promo codes are not allowed  
**When** a promo is already applied  
**Then** the system prevents adding another promo code and informs me of the limitation.

**Implementation**:
- UI shows applied promo code with a "Remove" button instead of input field
- Session stores only one promo code at a time
- User must remove current code before applying a new one

## API Endpoints

The cart page supports the following handlers:

### Apply Promo Code
**Handler**: `OnPostApplyPromoCodeAsync`
**Form Data**: `PromoCodeInput` (string)
**Response**: 
- Success: Redirects to cart with success message and applied code
- Error: Redirects to cart with error message

### Remove Promo Code
**Handler**: `OnPostRemovePromoCodeAsync`
**Response**: Redirects to cart with success message

## Session Management

Promo codes are stored in the HTTP session:
- **Key**: "AppliedPromoCode"
- **Value**: The promo code string
- **Scope**: Persists across cart and checkout pages
- **Cleanup**: Removed when cart is cleared or checkout is completed

## Validation Logic

The promo code validation performs the following checks (in order):

1. **Code exists**: Promo code must exist in database
2. **Is active**: `IsActive` must be true
3. **Not yet started**: Current time must be after `StartDate` (if set)
4. **Not expired**: Current time must be before `ExpirationDate` (if set)
5. **Usage limit**: `CurrentUsageCount` must be less than `MaximumUsageCount` (if set)
6. **Minimum order**: Cart subtotal must meet `MinimumOrderSubtotal` (if set)

For seller-specific codes:
7. **Seller products**: Only items from the specified store are included in discount calculation

## Discount Calculation

### Percentage Discounts
```
discount = applicable_subtotal * (discount_value / 100)
if (max_discount_amount is set):
    discount = min(discount, max_discount_amount)
```

### Fixed Amount Discounts
```
discount = discount_value
discount = min(discount, applicable_subtotal)  // Never exceed subtotal
```

### Seller-Specific Discounts
For seller-specific promo codes, only items from that seller's store are included in the `applicable_subtotal`.

### Total Calculation
```
total = items_subtotal + shipping - discount
total = max(total, 0)  // Ensure non-negative
```

## Security Considerations

- ✅ **Input validation**: All promo codes are validated before application
- ✅ **Server-side validation**: Validation occurs on the server, not just client-side
- ✅ **Re-validation at checkout**: Promo codes are re-validated before order placement
- ✅ **Usage tracking**: Usage counts are incremented atomically in the database
- ✅ **Session storage**: Promo codes are stored in secure HTTP sessions

## Future Enhancements (Not in Scope)

The current implementation supports the Phase 2 requirements. Future enhancements could include:

1. **Multiple promo codes**: Stack multiple compatible codes
2. **User-specific codes**: Codes that can only be used by specific users
3. **Product-specific codes**: Codes that apply only to certain products/categories
4. **Buy X Get Y**: More complex discount rules
5. **Referral codes**: Track which user referred the buyer
6. **Admin UI**: Management interface for creating/editing promo codes
7. **Analytics**: Track promo code usage and effectiveness
8. **Email integration**: Send promo codes via email campaigns

## Testing Instructions

### Enable the Feature
1. Set `"FeatureFlags:PromoCode": true` in `appsettings.Development.json`
2. Restart the application

### Test Scenario 1: Valid Platform Code
1. Login as buyer (buyer@test.com / Test123!)
2. Add items to cart (minimum $50 for SAVE20)
3. Go to cart page
4. Enter "SAVE20" in promo code field
5. Click "Apply"
6. ✅ Verify: Success message appears
7. ✅ Verify: Discount shows as -$10.00 (or 20% of subtotal, capped at $50)
8. ✅ Verify: Total is reduced by discount amount

### Test Scenario 2: Minimum Order Not Met
1. Login as buyer
2. Add items to cart (less than $50)
3. Go to cart page
4. Enter "SAVE20"
5. Click "Apply"
6. ✅ Verify: Error message shows minimum order requirement
7. ✅ Verify: No discount is applied

### Test Scenario 3: Invalid Code
1. Go to cart page
2. Enter "INVALIDCODE"
3. Click "Apply"
4. ✅ Verify: Error message appears
5. ✅ Verify: No discount is applied

### Test Scenario 4: Expired Code
1. Go to cart page
2. Enter "EXPIRED"
3. Click "Apply"
4. ✅ Verify: Error message appears
5. ✅ Verify: No discount is applied

### Test Scenario 5: Checkout Flow
1. Apply a valid promo code on cart page
2. Proceed to checkout
3. Complete address, shipping, and payment steps
4. Go to Review page
5. ✅ Verify: Discount is shown in order summary
6. ✅ Verify: Total includes the discount

### Test Scenario 6: Remove Code
1. Apply a valid promo code
2. ✅ Verify: Input field is replaced with applied code badge
3. Click "Remove" button
4. ✅ Verify: Success message appears
5. ✅ Verify: Input field is shown again
6. ✅ Verify: Discount is no longer applied

## Code Quality

The implementation follows MercatoApp coding standards:
- ✅ XML documentation on all public interfaces and methods
- ✅ Dependency injection for all services
- ✅ Async/await for database operations
- ✅ Proper error handling and logging
- ✅ Input validation and sanitization
- ✅ Consistent naming conventions
- ✅ Clean separation of concerns

## Database Schema

```sql
-- PromoCode table (simplified)
CREATE TABLE PromoCodes (
    Id INT PRIMARY KEY,
    Code NVARCHAR(450) UNIQUE NOT NULL,
    Scope INT NOT NULL,  -- 0=Platform, 1=Seller
    StoreId INT NULL,
    DiscountType INT NOT NULL,  -- 0=Percentage, 1=FixedAmount
    DiscountValue DECIMAL(18,2) NOT NULL,
    MinimumOrderSubtotal DECIMAL(18,2) NULL,
    MaximumDiscountAmount DECIMAL(18,2) NULL,
    StartDate DATETIME2 NULL,
    ExpirationDate DATETIME2 NULL,
    MaximumUsageCount INT NULL,
    CurrentUsageCount INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (StoreId) REFERENCES Stores(Id)
);

-- Indexes
CREATE UNIQUE INDEX IX_PromoCodes_Code ON PromoCodes(Code);
CREATE INDEX IX_PromoCodes_StoreId ON PromoCodes(StoreId);
CREATE INDEX IX_PromoCodes_IsActive_ExpirationDate ON PromoCodes(IsActive, ExpirationDate);
```

## Summary

The promo code feature is fully implemented and ready for Phase 2. It provides:
- ✅ Flexible discount rules (percentage or fixed amount)
- ✅ Platform-wide and seller-specific scopes
- ✅ Usage limits and expiration dates
- ✅ Minimum order requirements
- ✅ Maximum discount caps
- ✅ Clean user experience with validation feedback
- ✅ Seamless integration with cart and checkout flow
- ✅ Feature flag for easy enable/disable
- ✅ Comprehensive validation and security

The implementation is minimal, focused, and adheres to the existing codebase architecture and conventions.
