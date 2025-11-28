using MercatoApp.Authorization;
using MercatoApp.Data;
using MercatoApp.Models;
using MercatoApp.Services;
using Microsoft.AspNetCore.Authentication;
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
    options.UseInMemoryDatabase("MercatoDb"));

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
builder.Services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();
builder.Services.AddScoped<ISellerOnboardingService, SellerOnboardingService>();
builder.Services.AddScoped<IStoreProfileService, StoreProfileService>();

// Configure role-based authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyNames.BuyerOnly, policy =>
        policy.Requirements.Add(new RoleRequirement(Role.RoleNames.Buyer)));

    options.AddPolicy(PolicyNames.SellerOnly, policy =>
        policy.Requirements.Add(new RoleRequirement(Role.RoleNames.Seller)));

    options.AddPolicy(PolicyNames.AdminOnly, policy =>
        policy.Requirements.Add(new RoleRequirement(Role.RoleNames.Admin)));

    options.AddPolicy(PolicyNames.BuyerOrSeller, policy =>
        policy.Requirements.Add(new RoleRequirement(Role.RoleNames.Buyer, Role.RoleNames.Seller)));

    options.AddPolicy(PolicyNames.SellerOrAdmin, policy =>
        policy.Requirements.Add(new RoleRequirement(Role.RoleNames.Seller, Role.RoleNames.Admin)));
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

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
