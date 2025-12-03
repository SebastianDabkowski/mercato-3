namespace MercatoApp.Models;

/// <summary>
/// Types of critical actions that are audit logged.
/// </summary>
public enum AuditActionType
{
    // Authentication & Authorization
    Login,
    LoginFailed,
    Logout,
    PasswordReset,
    EmailVerificationSent,
    EmailVerified,
    
    // User Role Management
    RoleAssigned,
    RoleRevoked,
    PermissionGranted,
    PermissionRevoked,
    
    // Account Management
    UserRegistered,
    UserBlocked,
    UserUnblocked,
    AccountDeleted,
    AccountReactivated,
    UserProfileUpdated,
    
    // Store Management
    StoreCreated,
    StoreStatusChanged,
    StoreVerificationApproved,
    StoreVerificationRejected,
    
    // Payout & Financial
    PayoutMethodAdded,
    PayoutMethodUpdated,
    PayoutMethodDeleted,
    PayoutMethodSetDefault,
    PayoutScheduleCreated,
    PayoutScheduleUpdated,
    PayoutInitiated,
    PayoutCompleted,
    PayoutFailed,
    PayoutRetried,
    
    // Order Management
    OrderCreated,
    OrderStatusChanged,
    OrderStatusOverridden,
    OrderCancelled,
    
    // Refunds
    RefundRequested,
    RefundApproved,
    RefundRejected,
    RefundProcessed,
    RefundFailed,
    
    // Returns & Complaints
    ReturnRequested,
    ReturnApproved,
    ReturnRejected,
    ReturnCompleted,
    AdminCaseEscalated,
    AdminDecisionOverride,
    
    // Commission & Settlement
    CommissionRuleCreated,
    CommissionRuleUpdated,
    CommissionRuleDeleted,
    SettlementGenerated,
    SettlementAdjustmentAdded,
    
    // Product Moderation
    ProductApproved,
    ProductRejected,
    ProductFlagged,
    ProductUnflagged,
    
    // Review Moderation
    ReviewFlagged,
    ReviewApproved,
    ReviewRejected,
    ReviewDeleted,
    
    // Data Privacy & Compliance
    ConsentGranted,
    ConsentRevoked,
    DataExported,
    DataDeleted,
    SensitiveDataAccessed,
    
    // System Configuration
    FeatureFlagEnabled,
    FeatureFlagDisabled,
    IntegrationConfigured,
    IntegrationDisabled,
    LegalDocumentPublished,
    VatRuleCreated,
    VatRuleUpdated,
    CurrencyConfigUpdated,
    
    // Security
    SecurityAlertTriggered,
    SuspiciousActivityDetected,
    AccountLocked,
    AccountUnlocked
}
