using MercatoApp.Models;

namespace MercatoApp.Helpers;

/// <summary>
/// Helper class for recording user consent to legal documents.
/// Provides integration points for registration, checkout, and other consent scenarios.
/// </summary>
public static class ConsentHelper
{
    /// <summary>
    /// Records consent to required legal documents during user registration.
    /// </summary>
    /// <param name="legalDocumentService">The legal document service.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="ipAddress">The IP address from which consent was given.</param>
    /// <param name="userAgent">The user agent string.</param>
    /// <returns>Task representing the async operation.</returns>
    public static async Task RecordRegistrationConsentAsync(
        Services.ILegalDocumentService legalDocumentService,
        int userId,
        string? ipAddress,
        string? userAgent)
    {
        // Record consent to Terms of Service
        var tosDoc = await legalDocumentService.GetActiveDocumentAsync(LegalDocumentType.TermsOfService);
        if (tosDoc != null)
        {
            await legalDocumentService.RecordConsentAsync(
                userId, 
                tosDoc.Id, 
                ipAddress, 
                userAgent, 
                "registration");
        }

        // Record consent to Privacy Policy
        var privacyDoc = await legalDocumentService.GetActiveDocumentAsync(LegalDocumentType.PrivacyPolicy);
        if (privacyDoc != null)
        {
            await legalDocumentService.RecordConsentAsync(
                userId, 
                privacyDoc.Id, 
                ipAddress, 
                userAgent, 
                "registration");
        }

        // Record consent to Cookie Policy
        var cookieDoc = await legalDocumentService.GetActiveDocumentAsync(LegalDocumentType.CookiePolicy);
        if (cookieDoc != null)
        {
            await legalDocumentService.RecordConsentAsync(
                userId, 
                cookieDoc.Id, 
                ipAddress, 
                userAgent, 
                "registration");
        }
    }

    /// <summary>
    /// Records consent to seller agreement during seller onboarding.
    /// </summary>
    /// <param name="legalDocumentService">The legal document service.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="ipAddress">The IP address from which consent was given.</param>
    /// <param name="userAgent">The user agent string.</param>
    /// <returns>Task representing the async operation.</returns>
    public static async Task RecordSellerAgreementConsentAsync(
        Services.ILegalDocumentService legalDocumentService,
        int userId,
        string? ipAddress,
        string? userAgent)
    {
        var sellerDoc = await legalDocumentService.GetActiveDocumentAsync(LegalDocumentType.SellerAgreement);
        if (sellerDoc != null)
        {
            await legalDocumentService.RecordConsentAsync(
                userId, 
                sellerDoc.Id, 
                ipAddress, 
                userAgent, 
                "seller_onboarding");
        }
    }

    /// <summary>
    /// Records consent during checkout process.
    /// </summary>
    /// <param name="legalDocumentService">The legal document service.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="ipAddress">The IP address from which consent was given.</param>
    /// <param name="userAgent">The user agent string.</param>
    /// <returns>Task representing the async operation.</returns>
    public static async Task RecordCheckoutConsentAsync(
        Services.ILegalDocumentService legalDocumentService,
        int userId,
        string? ipAddress,
        string? userAgent)
    {
        // During checkout, users may need to re-confirm current terms
        var tosDoc = await legalDocumentService.GetActiveDocumentAsync(LegalDocumentType.TermsOfService);
        if (tosDoc != null)
        {
            // Only record if user hasn't already consented to this version
            var hasConsented = await legalDocumentService.HasUserConsentedAsync(
                userId, 
                LegalDocumentType.TermsOfService);
            
            if (!hasConsented)
            {
                await legalDocumentService.RecordConsentAsync(
                    userId, 
                    tosDoc.Id, 
                    ipAddress, 
                    userAgent, 
                    "checkout");
            }
        }
    }

    /// <summary>
    /// Checks if a user has consented to all required legal documents for a given context.
    /// </summary>
    /// <param name="legalDocumentService">The legal document service.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="context">The context (e.g., "registration", "seller_onboarding").</param>
    /// <returns>True if all required consents are present, false otherwise.</returns>
    public static async Task<bool> HasRequiredConsentsAsync(
        Services.ILegalDocumentService legalDocumentService,
        int userId,
        string context)
    {
        if (context == "seller_onboarding")
        {
            // Sellers need to consent to all documents including seller agreement
            return await legalDocumentService.HasUserConsentedAsync(userId, LegalDocumentType.TermsOfService)
                && await legalDocumentService.HasUserConsentedAsync(userId, LegalDocumentType.PrivacyPolicy)
                && await legalDocumentService.HasUserConsentedAsync(userId, LegalDocumentType.SellerAgreement);
        }
        else
        {
            // Regular users need to consent to ToS and Privacy Policy
            return await legalDocumentService.HasUserConsentedAsync(userId, LegalDocumentType.TermsOfService)
                && await legalDocumentService.HasUserConsentedAsync(userId, LegalDocumentType.PrivacyPolicy);
        }
    }
}
