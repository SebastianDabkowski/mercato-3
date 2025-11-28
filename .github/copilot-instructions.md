# Copilot Instructions for MercatoApp

## Project Overview

MercatoApp is a multi-vendor e-commerce marketplace built with ASP.NET Core 10 and Razor Pages. The platform connects independent online stores with buyers, featuring a Buyer Portal, Seller Panel, and Admin Back Office.

## Technology Stack

- **Framework**: ASP.NET Core 10 with Razor Pages
- **Database**: Entity Framework Core with In-Memory database (development)
- **Authentication**: Cookie-based authentication with Google and Facebook OAuth support
- **Frontend**: Bootstrap 5, static assets served via `wwwroot/`

## Build and Run Commands

```bash
# Build the application
dotnet build

# Run the application
dotnet run

# The app runs at https://localhost:5001 or http://localhost:5000
```

## Project Structure

- `Pages/` - Razor Pages (views and page models)
- `Pages/Account/` - Authentication-related pages (login, register, password reset)
- `Pages/Seller/` - Seller-specific pages (onboarding, dashboard)
- `Services/` - Business logic services (authentication, email, session management)
- `Models/` - Domain models (User, Store, Role, Session)
- `Data/` - Entity Framework DbContext
- `Authorization/` - Role-based authorization handlers and policies
- `Validation/` - Custom validation attributes
- `wwwroot/` - Static files (CSS, JavaScript, images)

## Code Conventions

- **Nullable Reference Types**: Enabled (`<Nullable>enable</Nullable>`)
- **Implicit Usings**: Enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- **Service Pattern**: Use dependency injection with interface-based services (e.g., `IUserAuthenticationService`, `IEmailService`)
- **Naming**: Use PascalCase for public members, prefix interfaces with `I`

## Security Considerations

- **CSRF Protection**: Anti-forgery tokens are required for form submissions
- **Authentication**: Cookie-based with secure settings (HttpOnly, SameSite)
- **Authorization**: Role-based policies defined in `PolicyNames.cs` (Buyer, Seller, Admin)
- **Session Management**: Custom session tokens with validation on each request

## User Roles

The application supports three user roles:
1. **Buyer** - Can browse products, make purchases
2. **Seller** - Can manage stores, products, and orders
3. **Admin** - Can manage users, moderate content, configure platform

## Key Services

When implementing new features, consider using these existing services:
- `IUserAuthenticationService` - Login/logout operations
- `IUserRegistrationService` - User registration
- `ISessionService` - Session management
- `IEmailService` - Email notifications
- `IRoleAuthorizationService` - Role-based access control
- `ISellerOnboardingService` - Seller registration flow
