using MercatoApp.Authorization;
using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authentication;
using MercatoApp; // For CommissionInvoiceTestScenario class
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add anti-forgery services for CSRF protection
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "MercatoAntiForgery";
    options.Cookie.HttpOnly = true;
    // Use SameAsRequest to work in both development (HTTP) and production (HTTPS)
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Add Entity Framework with In-Memory database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseInMemoryDatabase("MercatoDb");
    // Suppress transaction warnings for in-memory database
    options.ConfigureWarnings(warnings => 
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
});

// Add HTTP context accessor for cookie access
builder.Services.AddHttpContextAccessor();

// Add session support for anonymous cart
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "MercatoSession";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow HTTP in development
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromDays(7);
});

// Add application services
builder.Services.AddScoped<IPasswordValidationService, PasswordValidationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserRegistrationService, UserRegistrationService>();
builder.Services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
builder.Services.AddScoped<ISocialLoginService, SocialLoginService>();
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ILoginEventService, LoginEventService>();
builder.Services.AddScoped<IRoleAuthorizationService, RoleAuthorizationService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();
builder.Services.AddScoped<ISellerOnboardingService, SellerOnboardingService>();
builder.Services.AddScoped<IStoreProfileService, StoreProfileService>();
builder.Services.AddScoped<ISellerVerificationService, SellerVerificationService>();
builder.Services.AddScoped<IPayoutSettingsService, PayoutSettingsService>();
builder.Services.AddScoped<IInternalUserManagementService, InternalUserManagementService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductWorkflowService, ProductWorkflowService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICategoryAttributeService, CategoryAttributeService>();
builder.Services.AddScoped<IProductImageService, ProductImageService>();
builder.Services.AddScoped<IProductImportService, ProductImportService>();
builder.Services.AddScoped<IProductExportService, ProductExportService>();
builder.Services.AddScoped<IBulkProductUpdateService, BulkProductUpdateService>();
builder.Services.AddScoped<IProductVariantService, ProductVariantService>();
builder.Services.AddScoped<ISearchSuggestionService, SearchSuggestionService>();
builder.Services.AddScoped<IRecentlyViewedService, RecentlyViewedService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICartTotalsService, CartTotalsService>();
builder.Services.AddScoped<IGuestCartService, GuestCartService>();
builder.Services.AddScoped<IPromoCodeService, PromoCodeService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderStatusService, OrderStatusService>();
builder.Services.AddScoped<IOrderItemFulfillmentService, OrderItemFulfillmentService>();
builder.Services.AddScoped<IOrderExportService, OrderExportService>();
builder.Services.AddScoped<IReturnRequestService, ReturnRequestService>();
builder.Services.AddScoped<ISLAService, SLAService>();
builder.Services.AddScoped<IShippingMethodService, ShippingMethodService>();
builder.Services.AddScoped<IShippingLabelService, ShippingLabelService>();
builder.Services.AddScoped<IShippingProviderIntegrationService, ShippingProviderIntegrationService>();
builder.Services.AddScoped<IProductReviewService, ProductReviewService>();
builder.Services.AddScoped<IReviewModerationService, ReviewModerationService>();
builder.Services.AddScoped<IProductModerationService, ProductModerationService>();
builder.Services.AddScoped<IPhotoModerationService, PhotoModerationService>();
builder.Services.AddScoped<ISellerRatingService, SellerRatingService>();
builder.Services.AddScoped<ISellerRatingModerationService, SellerRatingModerationService>();
builder.Services.AddScoped<ISellerReputationService, SellerReputationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
builder.Services.AddScoped<IProductQuestionService, ProductQuestionService>();
builder.Services.AddScoped<IOrderMessageService, OrderMessageService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<ISellerDashboardService, SellerDashboardService>();
builder.Services.AddScoped<IAdminReportService, AdminReportService>();
builder.Services.AddScoped<ISellerRevenueReportService, SellerRevenueReportService>();
builder.Services.AddScoped<IUserAnalyticsService, UserAnalyticsService>();
builder.Services.AddScoped<IAnalyticsEventService, AnalyticsEventService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IAdminAuditLogService, AdminAuditLogService>();

// Register shipping provider services as singleton collection
builder.Services.AddSingleton<IShippingProviderService>(sp => 
    new MockShippingProviderService(
        sp.GetRequiredService<ILogger<MockShippingProviderService>>(),
        "mock_standard"));
builder.Services.AddSingleton<IShippingProviderService>(sp => 
    new MockShippingProviderService(
        sp.GetRequiredService<ILogger<MockShippingProviderService>>(),
        "mock_express"));

builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentProviderService, MockPaymentProviderService>();
builder.Services.AddScoped<IRefundService, RefundService>();
builder.Services.AddScoped<ICommissionService, CommissionService>();
builder.Services.AddScoped<ICommissionRuleService, CommissionRuleService>();
builder.Services.AddScoped<ICommissionInvoiceService, CommissionInvoiceService>();
builder.Services.AddScoped<IEscrowService, EscrowService>();
builder.Services.AddScoped<IPayoutService, PayoutService>();
builder.Services.AddScoped<ISettlementService, SettlementService>();
builder.Services.AddScoped<IVatRuleService, VatRuleService>();
builder.Services.AddScoped<ICurrencyService, CurrencyService>();
builder.Services.AddScoped<IIntegrationService, IntegrationService>();
builder.Services.AddScoped<ILegalDocumentService, LegalDocumentService>();
builder.Services.AddScoped<IFeatureFlagService, FeatureFlagService>();
builder.Services.AddScoped<IFeatureFlagManagementService, FeatureFlagManagementService>();
builder.Services.AddScoped<IProcessingActivityService, ProcessingActivityService>();

// Add background services
builder.Services.AddHostedService<MercatoApp.Helpers.SLAMonitoringService>();

// Configure role-based authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyNames.BuyerOnly, policy =>
        policy.Requirements.Add(new RoleRequirement(Role.RoleNames.Buyer)));

    options.AddPolicy(PolicyNames.SellerOnly, policy =>
        policy.Requirements.Add(new RoleRequirement(Role.RoleNames.Seller)));

    options.AddPolicy(PolicyNames.AdminOnly, policy =>
        policy.Requirements.Add(new RoleRequirement(Role.RoleNames.Admin)));

    options.AddPolicy(PolicyNames.SupportOnly, policy =>
        policy.Requirements.Add(new RoleRequirement(Role.RoleNames.Support)));

    options.AddPolicy(PolicyNames.ComplianceOnly, policy =>
        policy.Requirements.Add(new RoleRequirement(Role.RoleNames.Compliance)));

    options.AddPolicy(PolicyNames.BuyerOrSeller, policy =>
        policy.Requirements.Add(new RoleRequirement(Role.RoleNames.Buyer, Role.RoleNames.Seller)));

    options.AddPolicy(PolicyNames.SellerOrAdmin, policy =>
        policy.Requirements.Add(new RoleRequirement(Role.RoleNames.Seller, Role.RoleNames.Admin)));

    options.AddPolicy(PolicyNames.AdminOrSupportOrCompliance, policy =>
        policy.Requirements.Add(new RoleRequirement(Role.RoleNames.Admin, Role.RoleNames.Support, Role.RoleNames.Compliance)));
});

// Configure authentication with cookie and external OAuth providers
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "MercatoAuth";
    options.Cookie.HttpOnly = true; // Protects against XSS
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS only
    options.Cookie.SameSite = SameSiteMode.Lax; // Required for OAuth redirects, provides CSRF protection
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    
    // Validate session token on each request
    options.Events.OnValidatePrincipal = async context =>
    {
        var sessionToken = context.Principal?.FindFirst("SessionToken")?.Value;
        if (!string.IsNullOrEmpty(sessionToken))
        {
            var sessionService = context.HttpContext.RequestServices.GetRequiredService<ISessionService>();
            var validationResult = await sessionService.ValidateSessionAsync(sessionToken);
            
            if (!validationResult.IsValid)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            else
            {
                // Update last access time for sliding expiration
                await sessionService.UpdateSessionAccessTimeAsync(sessionToken);
            }
        }
    };
});

// Configure Google OAuth if credentials are provided
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/signin-google";
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.SaveTokens = false;
    });
}

// Configure Facebook OAuth if credentials are provided
var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
if (!string.IsNullOrEmpty(facebookAppId) && !string.IsNullOrEmpty(facebookAppSecret))
{
    authBuilder.AddFacebook(FacebookDefaults.AuthenticationScheme, options =>
    {
        options.AppId = facebookAppId;
        options.AppSecret = facebookAppSecret;
        options.CallbackPath = "/signin-facebook";
        options.Scope.Add("email");
        options.Scope.Add("public_profile");
        options.SaveTokens = false;
        options.Fields.Add("email");
        options.Fields.Add("first_name");
        options.Fields.Add("last_name");
    });
}

var app = builder.Build();

// Seed test data in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await TestDataSeeder.SeedTestDataAsync(context);

    // Run integration management test scenario
    var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();
    await IntegrationManagementTestScenario.RunTestAsync(context, integrationService);

    // Run commission invoice test scenario
    var invoiceService = scope.ServiceProvider.GetRequiredService<ICommissionInvoiceService>();
    var commissionService = scope.ServiceProvider.GetRequiredService<ICommissionService>();
    await CommissionInvoiceTestScenario.RunTestAsync(context, invoiceService, commissionService);

    // Run return/complaint request test scenario
    var returnRequestService = scope.ServiceProvider.GetRequiredService<IReturnRequestService>();
    await ReturnComplaintTestScenario.RunTestAsync(context, returnRequestService);

    // Run case resolution and refund linkage test scenario
    var refundService = scope.ServiceProvider.GetRequiredService<IRefundService>();
    await CaseResolutionTestScenario.RunTestAsync(context, returnRequestService, refundService);

    // Run SLA tracking test scenario
    var slaService = scope.ServiceProvider.GetRequiredService<ISLAService>();
    await SLATrackingTestScenario.RunTestAsync(context, returnRequestService, slaService);

    // Run seller reputation test scenario
    var reputationService = scope.ServiceProvider.GetRequiredService<ISellerReputationService>();
    await SellerReputationTestScenario.RunTestAsync(context, reputationService);

    // Run comprehensive reputation test with sample data
    await SellerReputationComprehensiveTest.RunTestAsync(context, reputationService);

    // Run seller dashboard test scenario
    var dashboardService = scope.ServiceProvider.GetRequiredService<ISellerDashboardService>();
    var dashboardTest = new SellerDashboardTestScenario(context, dashboardService);
    await dashboardTest.RunAsync();

    // Run feature flag test scenario
    var flagManagementService = scope.ServiceProvider.GetRequiredService<IFeatureFlagManagementService>();
    var flagRuntimeService = scope.ServiceProvider.GetRequiredService<IFeatureFlagService>();
    await FeatureFlagTestScenario.RunTestAsync(context, flagManagementService, flagRuntimeService);

    // Run RBAC test scenario
    var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
    await RbacTestScenario.RunTestAsync(context, permissionService);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
