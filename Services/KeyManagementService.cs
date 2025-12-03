using System.Security.Cryptography;
using System.Text;

namespace MercatoApp.Services;

/// <summary>
/// Configuration for the key management service.
/// </summary>
public class KeyManagementConfig
{
    /// <summary>
    /// Gets or sets the master encryption key (Base64-encoded).
    /// In production, this should be stored in a secure key vault (Azure Key Vault, AWS KMS, etc.).
    /// </summary>
    public string MasterKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current active key version.
    /// Incremented when keys are rotated.
    /// </summary>
    public int CurrentKeyVersion { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of days after which keys should be rotated.
    /// Default is 90 days as per security best practices.
    /// </summary>
    public int KeyRotationDays { get; set; } = 90;

    /// <summary>
    /// Gets or sets the last key rotation date.
    /// </summary>
    public DateTime? LastKeyRotation { get; set; }
}

/// <summary>
/// Interface for key management service that manages encryption keys and rotation.
/// </summary>
public interface IKeyManagementService
{
    /// <summary>
    /// Gets the encryption key for the current version.
    /// </summary>
    /// <returns>The encryption key as a byte array.</returns>
    byte[] GetCurrentKey();

    /// <summary>
    /// Gets the encryption key for a specific version.
    /// </summary>
    /// <param name="version">The key version to retrieve.</param>
    /// <returns>The encryption key as a byte array.</returns>
    byte[] GetKey(int version);

    /// <summary>
    /// Gets the current active key version.
    /// </summary>
    int GetCurrentKeyVersion();

    /// <summary>
    /// Checks if key rotation is needed based on configuration.
    /// </summary>
    bool IsKeyRotationNeeded();

    /// <summary>
    /// Generates a new encryption key and increments the key version.
    /// This should be called during key rotation procedures.
    /// </summary>
    /// <returns>The new key version.</returns>
    Task<int> RotateKeyAsync();
}

/// <summary>
/// Service for managing encryption keys with support for key rotation.
/// In production, this should integrate with a managed KMS like Azure Key Vault or AWS KMS.
/// This implementation provides a foundation that can be extended with cloud KMS integration.
/// </summary>
public class KeyManagementService : IKeyManagementService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeyManagementService> _logger;
    private readonly Dictionary<int, byte[]> _keyCache = new();
    private readonly object _lockObject = new();

    public KeyManagementService(
        IConfiguration configuration,
        ILogger<KeyManagementService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        InitializeKeys();
    }

    private void InitializeKeys()
    {
        // In production, load keys from Azure Key Vault, AWS KMS, or similar
        // For now, derive keys from configuration or generate secure keys
        var masterKeyBase64 = _configuration["Encryption:MasterKey"];
        
        if (string.IsNullOrEmpty(masterKeyBase64))
        {
            _logger.LogWarning("No master encryption key found in configuration. Generating temporary key for development.");
            // Generate a secure random key for development only
            // In production, this must come from a secure key vault
            var tempKey = new byte[32]; // 256 bits for AES-256
            RandomNumberGenerator.Fill(tempKey);
            masterKeyBase64 = Convert.ToBase64String(tempKey);
        }

        var masterKey = Convert.FromBase64String(masterKeyBase64);
        var currentVersion = _configuration.GetValue<int>("Encryption:CurrentKeyVersion", 1);
        
        // Derive versioned keys from master key using HKDF
        for (int version = 1; version <= currentVersion; version++)
        {
            _keyCache[version] = DeriveKey(masterKey, version);
        }

        _logger.LogInformation("Initialized key management service with {KeyCount} key versions", _keyCache.Count);
    }

    /// <inheritdoc />
    public byte[] GetCurrentKey()
    {
        var version = GetCurrentKeyVersion();
        return GetKey(version);
    }

    /// <inheritdoc />
    public byte[] GetKey(int version)
    {
        lock (_lockObject)
        {
            if (_keyCache.TryGetValue(version, out var key))
            {
                return key;
            }

            throw new InvalidOperationException($"Encryption key version {version} not found. The data may have been encrypted with a rotated key that is no longer available.");
        }
    }

    /// <inheritdoc />
    public int GetCurrentKeyVersion()
    {
        return _configuration.GetValue<int>("Encryption:CurrentKeyVersion", 1);
    }

    /// <inheritdoc />
    public bool IsKeyRotationNeeded()
    {
        var rotationDays = _configuration.GetValue<int>("Encryption:KeyRotationDays", 90);
        var lastRotationStr = _configuration["Encryption:LastKeyRotation"];
        
        if (string.IsNullOrEmpty(lastRotationStr))
        {
            // No rotation has occurred yet, check if we should do initial rotation
            return false;
        }

        if (DateTime.TryParse(lastRotationStr, out var lastRotation))
        {
            return (DateTime.UtcNow - lastRotation).TotalDays >= rotationDays;
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<int> RotateKeyAsync()
    {
        // In production, this should:
        // 1. Generate new key in KMS
        // 2. Update configuration in secure storage
        // 3. Trigger background job to re-encrypt data with new key
        // 4. Maintain old keys for decryption of existing data
        
        var currentVersion = GetCurrentKeyVersion();
        var newVersion = currentVersion + 1;

        _logger.LogWarning("Key rotation is initiated. New key version: {NewVersion}. " +
            "In production, this should integrate with a managed KMS and trigger data re-encryption.", 
            newVersion);

        // Generate new key
        var masterKeyBase64 = _configuration["Encryption:MasterKey"];
        if (string.IsNullOrEmpty(masterKeyBase64))
        {
            throw new InvalidOperationException("Cannot rotate keys without a master key configured.");
        }

        var masterKey = Convert.FromBase64String(masterKeyBase64);
        var newKey = DeriveKey(masterKey, newVersion);

        lock (_lockObject)
        {
            _keyCache[newVersion] = newKey;
        }

        _logger.LogInformation("Key rotation completed. New version: {NewVersion}", newVersion);

        // Note: In production, update configuration in secure storage
        // This is a placeholder - actual implementation should update Azure Key Vault, AWS Secrets Manager, etc.
        
        return await Task.FromResult(newVersion);
    }

    /// <summary>
    /// Derives a versioned key from the master key using HKDF (HMAC-based Key Derivation Function).
    /// </summary>
    private static byte[] DeriveKey(byte[] masterKey, int version)
    {
        // Use HKDF to derive a version-specific key
        // Info parameter includes version to ensure different keys for each version
        var info = Encoding.UTF8.GetBytes($"MercatoApp-v{version}");
        var salt = Encoding.UTF8.GetBytes("MercatoApp-Salt"); // In production, use a secure random salt
        
        var derivedKey = new byte[32]; // 256 bits for AES-256
        
        using var hkdf = new HMACSHA256(masterKey);
        var prk = hkdf.ComputeHash(salt.Concat(new byte[] { 0 }).ToArray());
        
        using var hkdf2 = new HMACSHA256(prk);
        var okm = hkdf2.ComputeHash(info.Concat(new byte[] { 1 }).ToArray());
        Array.Copy(okm, derivedKey, 32);
        
        return derivedKey;
    }
}
