# Data Encryption Implementation

## Overview

This document describes the encryption at rest and in transit implementation for MercatoApp, designed to meet security and compliance requirements for protecting personal and sensitive data.

## Encryption at Rest

### Implementation

MercatoApp implements field-level encryption for sensitive data using industry-standard AES-256-GCM (Galois/Counter Mode) encryption:

- **Algorithm**: AES-256-GCM (AEAD - Authenticated Encryption with Associated Data)
- **Key Size**: 256 bits
- **Authentication Tag**: 128 bits
- **Nonce**: 96 bits (randomly generated per encryption operation)
- **Key Derivation**: HKDF (HMAC-based Key Derivation Function) with SHA-256

### Encrypted Fields

The following fields are encrypted at the application level:

1. **Financial Data**:
   - `PayoutMethod.BankAccountNumberEncrypted` - Bank account numbers
   - `Store.BankAccountNumber` - Store bank account information

2. **API Credentials**:
   - `Integration.ApiKey` - Third-party integration API keys
   - `ShippingProviderConfig.ApiKey` - Shipping provider API keys
   - `ShippingProviderConfig.ApiSecret` - Shipping provider API secrets

3. **Personal Identifiers**:
   - `User.TaxId` - Tax identification numbers (when implemented)
   - `User.TwoFactorSecretKey` - TOTP secret keys for 2FA (when implemented)

### Services

#### DataEncryptionService

The `DataEncryptionService` provides encryption and decryption operations:

```csharp
public interface IDataEncryptionService
{
    string? Encrypt(string? plainText);
    string? Decrypt(string? cipherText);
    string? EncryptWithKeyVersion(string? plainText, int keyVersion);
    int GetCurrentKeyVersion();
}
```

**Features**:
- Automatic key version tagging for seamless key rotation
- Authenticated encryption prevents tampering
- Null-safe operations
- Exception handling with detailed logging

#### KeyManagementService

The `KeyManagementService` manages encryption keys and supports key rotation:

```csharp
public interface IKeyManagementService
{
    byte[] GetCurrentKey();
    byte[] GetKey(int version);
    int GetCurrentKeyVersion();
    bool IsKeyRotationNeeded();
    Task<int> RotateKeyAsync();
}
```

**Features**:
- Versioned keys for backward compatibility during rotation
- HKDF-based key derivation from master key
- Support for multiple key versions simultaneously
- Integration-ready for cloud KMS (Azure Key Vault, AWS KMS)

### Configuration

Encryption configuration is stored in `appsettings.json` (development) or secure configuration providers (production):

```json
{
  "Encryption": {
    "MasterKey": "<Base64-encoded-master-key>",
    "CurrentKeyVersion": 1,
    "KeyRotationDays": 90,
    "LastKeyRotation": "2025-01-01T00:00:00Z"
  }
}
```

**Production Requirements**:
- Master keys MUST be stored in a managed key vault (Azure Key Vault, AWS KMS, Google Cloud KMS)
- Never commit encryption keys to source control
- Use environment-specific configuration sources
- Enable automatic key rotation where supported by the KMS provider

### Key Rotation

#### Rotation Schedule

- **Recommended Interval**: 90 days (configurable via `KeyRotationDays`)
- **Process**: Automated via `KeyManagementService.RotateKeyAsync()`

#### Rotation Procedure

1. **Generate New Key**:
   - New key version is created (e.g., version 2)
   - Key is derived from master key with new version parameter
   - New key version is stored in key cache

2. **Update Configuration**:
   - `CurrentKeyVersion` is incremented
   - `LastKeyRotation` timestamp is updated
   - Configuration is saved to secure storage

3. **Gradual Migration**:
   - New encryptions use the new key version
   - Old data remains encrypted with previous key versions
   - Decryption automatically uses the correct key version based on metadata
   - Background job re-encrypts data with new key (optional, recommended)

4. **Old Key Retention**:
   - Previous key versions are retained for decryption
   - Keys should be retained for the duration of your data retention policy
   - Expired keys can be removed after all data is re-encrypted

#### Manual Key Rotation

For emergency key rotation (e.g., suspected compromise):

1. Generate a new master key in your KMS
2. Update the `Encryption:MasterKey` configuration
3. Increment `CurrentKeyVersion`
4. Run key rotation: `await keyManagementService.RotateKeyAsync()`
5. Trigger background re-encryption job for all encrypted fields
6. Revoke the old master key once re-encryption is complete

## Encryption in Transit

### HTTPS/TLS Configuration

All traffic is encrypted in transit using HTTPS/TLS:

#### Production Settings

```csharp
// HSTS Configuration (HTTP Strict Transport Security)
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);  // 1 year
    options.IncludeSubDomains = true;
    options.Preload = true;
});

// HTTPS Redirection - Forces all HTTP traffic to HTTPS
app.UseHttpsRedirection();
```

#### Cookie Security

All cookies are configured with secure flags in production:

- **Secure Flag**: Cookies only sent over HTTPS (`CookieSecurePolicy.Always`)
- **HttpOnly Flag**: Prevents JavaScript access to cookies
- **SameSite**: Set to `Strict` or `Lax` for CSRF protection

```csharp
// Anti-forgery cookies
options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
    ? CookieSecurePolicy.SameAsRequest 
    : CookieSecurePolicy.Always;

// Authentication cookies
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

// Session cookies
options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
    ? CookieSecurePolicy.None 
    : CookieSecurePolicy.Always;
```

### TLS Version Requirements

**Production Requirements**:
- Minimum TLS version: TLS 1.2
- Recommended: TLS 1.3 where supported
- Disable SSL 3.0, TLS 1.0, and TLS 1.1 (deprecated and insecure)

**Cipher Suite Requirements**:
- Use only strong cipher suites
- Prefer AEAD ciphers (e.g., AES-GCM)
- Disable weak ciphers (RC4, DES, 3DES)
- Enable Perfect Forward Secrecy (PFS)

### Certificate Management

**Production Checklist**:
- [ ] Use certificates from a trusted Certificate Authority (CA)
- [ ] Implement automatic certificate renewal
- [ ] Enable OCSP stapling for revocation checking
- [ ] Configure CAA DNS records
- [ ] Monitor certificate expiration dates

## Database Encryption

### In-Memory Database (Development)

The application currently uses an in-memory database for development. No at-rest encryption is needed for the in-memory provider as data is ephemeral.

### Production Database Recommendations

For production deployments with SQL Server, PostgreSQL, or MySQL:

1. **Transparent Data Encryption (TDE)**:
   - Enable TDE at the database level for storage-level encryption
   - SQL Server: Use TDE with Certificate or Asymmetric Key
   - PostgreSQL: Enable transparent data encryption extension
   - Azure SQL: Enable TDE (on by default)
   - AWS RDS: Enable encryption at rest in DB instance settings

2. **Connection String Encryption**:
   - Always use encrypted connections (`Encrypt=True` for SQL Server)
   - Validate server certificates
   - Store connection strings in secure configuration (Azure Key Vault, AWS Secrets Manager)

3. **Backup Encryption**:
   - Enable encryption for database backups
   - Rotate backup encryption keys according to policy

## File Storage Encryption

For production deployments with file storage:

### Azure Blob Storage
- Enable encryption at rest (default)
- Use customer-managed keys in Azure Key Vault
- Enable HTTPS-only traffic

### AWS S3
- Enable default encryption (SSE-S3 or SSE-KMS)
- Use S3 bucket policies to enforce encryption
- Enable bucket versioning for data protection

### Google Cloud Storage
- Enable default encryption
- Use customer-managed encryption keys (CMEK)
- Configure HTTPS-only access

## Compliance and Audit

### Encryption Standards

The implementation meets or exceeds the following standards:

- **PCI DSS**: Requirements 3.4 (encryption of PAN), 4.1 (encrypted transmission)
- **GDPR**: Article 32 (security of processing, encryption)
- **HIPAA**: 164.312(a)(2)(iv) (encryption and decryption)
- **SOC 2**: CC6.1, CC6.7 (logical and physical access controls, data security)

### Audit Trail

All encryption operations are logged:

- Key version used for encryption
- Encryption/decryption failures
- Key rotation events
- Configuration changes

**Log Retention**:
- Security logs: Minimum 90 days
- Audit logs: As required by compliance requirements (typically 1-7 years)

### Monitoring and Alerts

Implement monitoring for:

- [ ] Failed decryption attempts (potential tampering)
- [ ] Key rotation failures
- [ ] Certificate expiration warnings (30, 14, 7 days before expiration)
- [ ] TLS/SSL connection failures
- [ ] Unusual patterns in encrypted data access

## Incident Response

### Suspected Key Compromise

1. **Immediate Actions**:
   - Rotate encryption keys immediately
   - Review audit logs for suspicious activity
   - Notify security team and stakeholders

2. **Investigation**:
   - Determine scope of compromise
   - Identify affected data
   - Review access logs

3. **Remediation**:
   - Re-encrypt all affected data with new keys
   - Revoke compromised keys
   - Update security controls to prevent recurrence

4. **Notification**:
   - Follow data breach notification requirements
   - Document incident for compliance audits

### Data Breach Response

1. Assess scope of exposed data
2. Determine if data was encrypted
3. Follow organizational breach response procedures
4. Notify affected parties as required by law
5. Document lessons learned and implement improvements

## Testing and Validation

### Security Testing

Before production deployment:

- [ ] Verify encryption is working correctly
- [ ] Test key rotation procedure
- [ ] Validate TLS/HTTPS enforcement
- [ ] Confirm secure cookie settings
- [ ] Test decryption with multiple key versions
- [ ] Verify encrypted data cannot be read directly from database

### Penetration Testing

Include the following in regular security assessments:

- [ ] Attempt to access encrypted data without proper authentication
- [ ] Test for weak cipher suites
- [ ] Verify HTTPS enforcement and HSTS
- [ ] Check for exposed encryption keys in configuration or logs
- [ ] Test key rotation and backward compatibility

## Production Deployment Checklist

Before deploying to production:

- [ ] Generate secure master encryption key and store in KMS
- [ ] Configure encryption settings in production configuration
- [ ] Remove development keys from production configuration
- [ ] Enable TLS 1.2+ and disable older protocols
- [ ] Configure HSTS with appropriate max-age
- [ ] Enable database encryption at rest (TDE)
- [ ] Set up certificate auto-renewal
- [ ] Configure monitoring and alerting
- [ ] Document key rotation procedures in runbook
- [ ] Train operations team on incident response
- [ ] Complete security audit/penetration test
- [ ] Verify backup encryption is enabled

## Cloud Provider Integration

### Azure Key Vault Integration (Recommended for Azure)

```csharp
// Add Azure Key Vault configuration provider
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());

// Access secrets directly from Key Vault
var masterKey = builder.Configuration["Encryption--MasterKey"];
```

### AWS Secrets Manager Integration (Recommended for AWS)

```csharp
// Add AWS Secrets Manager configuration provider
builder.Configuration.AddSecretsManager(
    configurator: options =>
    {
        options.SecretFilter = secret => secret.Name.StartsWith("MercatoApp/");
    });

// Access secrets
var masterKey = builder.Configuration["MercatoApp/Encryption/MasterKey"];
```

### Google Cloud Secret Manager (Recommended for GCP)

```csharp
// Add Google Cloud Secret Manager configuration provider
builder.Configuration.AddGoogleSecretManager(
    projectId: "your-project-id",
    secretPrefix: "MercatoApp");

// Access secrets
var masterKey = builder.Configuration["Encryption__MasterKey"];
```

## Support and Maintenance

### Key Rotation Schedule

- **Regular Rotation**: Every 90 days (automated)
- **Emergency Rotation**: As needed for security incidents
- **Audit**: Quarterly review of encryption configuration

### Maintenance Tasks

- Monthly: Review encryption logs for anomalies
- Quarterly: Audit key rotation compliance
- Annually: Review and update encryption standards
- As needed: Update cipher suites and TLS versions

## References

- [NIST SP 800-57: Key Management Recommendations](https://csrc.nist.gov/publications/detail/sp/800-57-part-1/rev-5/final)
- [OWASP Cryptographic Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cryptographic_Storage_Cheat_Sheet.html)
- [Azure Key Vault Best Practices](https://docs.microsoft.com/en-us/azure/key-vault/general/best-practices)
- [AWS KMS Best Practices](https://docs.aws.amazon.com/kms/latest/developerguide/best-practices.html)
- [TLS/SSL Best Practices](https://wiki.mozilla.org/Security/Server_Side_TLS)
