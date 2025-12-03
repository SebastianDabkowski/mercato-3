namespace MercatoApp.Services;

/// <summary>
/// Interface for data encryption service that provides field-level encryption for sensitive data.
/// Uses AES-256-GCM encryption with managed keys.
/// </summary>
public interface IDataEncryptionService
{
    /// <summary>
    /// Encrypts a string value using the current encryption key.
    /// </summary>
    /// <param name="plainText">The plain text to encrypt.</param>
    /// <returns>The encrypted value as a Base64-encoded string, or null if input is null.</returns>
    string? Encrypt(string? plainText);

    /// <summary>
    /// Decrypts an encrypted string value.
    /// </summary>
    /// <param name="cipherText">The Base64-encoded encrypted value.</param>
    /// <returns>The decrypted plain text, or null if input is null.</returns>
    string? Decrypt(string? cipherText);

    /// <summary>
    /// Encrypts a string value using a specific key version.
    /// Used during key rotation to encrypt with the new key.
    /// </summary>
    /// <param name="plainText">The plain text to encrypt.</param>
    /// <param name="keyVersion">The key version to use for encryption.</param>
    /// <returns>The encrypted value as a Base64-encoded string, or null if input is null.</returns>
    string? EncryptWithKeyVersion(string? plainText, int keyVersion);

    /// <summary>
    /// Gets the current active key version.
    /// </summary>
    int GetCurrentKeyVersion();
}
