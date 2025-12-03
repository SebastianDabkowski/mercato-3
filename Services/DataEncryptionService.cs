using System.Security.Cryptography;
using System.Text;

namespace MercatoApp.Services;

/// <summary>
/// Service for encrypting and decrypting sensitive data fields.
/// Uses AES-256-GCM for authenticated encryption with associated data (AEAD).
/// Supports key versioning for seamless key rotation.
/// </summary>
public class DataEncryptionService : IDataEncryptionService
{
    private readonly IKeyManagementService _keyManagement;
    private readonly ILogger<DataEncryptionService> _logger;
    
    // Encryption metadata structure: [Version:1byte][Nonce:12bytes][Tag:16bytes][CipherText:variable]
    private const int VersionSize = 1;
    private const int NonceSize = 12;  // 96 bits recommended for GCM
    private const int TagSize = 16;    // 128 bits authentication tag
    private const int HeaderSize = VersionSize + NonceSize + TagSize;

    public DataEncryptionService(
        IKeyManagementService keyManagement,
        ILogger<DataEncryptionService> logger)
    {
        _keyManagement = keyManagement;
        _logger = logger;
    }

    /// <inheritdoc />
    public string? Encrypt(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        var keyVersion = _keyManagement.GetCurrentKeyVersion();
        return EncryptWithKeyVersion(plainText, keyVersion);
    }

    /// <inheritdoc />
    public string? EncryptWithKeyVersion(string? plainText, int keyVersion)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        try
        {
            var key = _keyManagement.GetKey(keyVersion);
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            
            // Generate random nonce (12 bytes for GCM)
            var nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            // Prepare output buffer: [Version][Nonce][Tag][CipherText]
            var cipherTextBytes = new byte[plainTextBytes.Length];
            var tag = new byte[TagSize];

            // Encrypt using AES-GCM
            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Encrypt(nonce, plainTextBytes, cipherTextBytes, tag);

            // Combine version, nonce, tag, and ciphertext
            // Version is stored as byte, so key versions must be <= 255
            if (keyVersion > 255)
            {
                throw new InvalidOperationException($"Key version {keyVersion} exceeds maximum supported version (255).");
            }
            
            var result = new byte[HeaderSize + cipherTextBytes.Length];
            result[0] = (byte)keyVersion;
            Buffer.BlockCopy(nonce, 0, result, VersionSize, NonceSize);
            Buffer.BlockCopy(tag, 0, result, VersionSize + NonceSize, TagSize);
            Buffer.BlockCopy(cipherTextBytes, 0, result, HeaderSize, cipherTextBytes.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data with key version {KeyVersion}", keyVersion);
            throw new InvalidOperationException("Encryption failed. See logs for details.", ex);
        }
    }

    /// <inheritdoc />
    public string? Decrypt(string? cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return cipherText;
        }

        try
        {
            var encryptedData = Convert.FromBase64String(cipherText);

            if (encryptedData.Length < HeaderSize)
            {
                throw new InvalidOperationException("Invalid encrypted data format.");
            }

            // Extract version, nonce, tag, and ciphertext
            var keyVersion = encryptedData[0];
            var nonce = new byte[NonceSize];
            var tag = new byte[TagSize];
            var cipherTextBytes = new byte[encryptedData.Length - HeaderSize];

            Buffer.BlockCopy(encryptedData, VersionSize, nonce, 0, NonceSize);
            Buffer.BlockCopy(encryptedData, VersionSize + NonceSize, tag, 0, TagSize);
            Buffer.BlockCopy(encryptedData, HeaderSize, cipherTextBytes, 0, cipherTextBytes.Length);

            // Get the appropriate key version
            var key = _keyManagement.GetKey(keyVersion);

            // Decrypt using AES-GCM
            var plainTextBytes = new byte[cipherTextBytes.Length];
            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Decrypt(nonce, cipherTextBytes, tag, plainTextBytes);

            return Encoding.UTF8.GetString(plainTextBytes);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Failed to decrypt data. Data may be corrupted or tampered with.");
            throw new InvalidOperationException("Decryption failed. Data may be corrupted or authentication failed.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data");
            throw new InvalidOperationException("Decryption failed. See logs for details.", ex);
        }
    }

    /// <inheritdoc />
    public int GetCurrentKeyVersion()
    {
        return _keyManagement.GetCurrentKeyVersion();
    }
}
