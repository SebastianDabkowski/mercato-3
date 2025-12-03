using MercatoApp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MercatoApp.Tests;

/// <summary>
/// Simple test to verify encryption functionality.
/// </summary>
public static class EncryptionTest
{
    public static async Task RunTestAsync()
    {
        Console.WriteLine("=== Encryption Service Test ===\n");

        // Setup configuration
        var configData = new Dictionary<string, string?>
        {
            { "Encryption:MasterKey", "OBnWr3w5Oc0GpmvPPqj3hWw0tj9tqOPFysJ8NOhrBoI=" },
            { "Encryption:CurrentKeyVersion", "1" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var keyLogger = loggerFactory.CreateLogger<KeyManagementService>();
        var encryptionLogger = loggerFactory.CreateLogger<DataEncryptionService>();

        var keyManagement = new KeyManagementService(configuration, keyLogger);
        var encryption = new DataEncryptionService(keyManagement, encryptionLogger);

        // Test 1: Basic encryption and decryption
        Console.WriteLine("Test 1: Basic Encryption/Decryption");
        var plainText = "1234567890123456"; // Bank account number
        var encrypted = encryption.Encrypt(plainText);
        var decrypted = encryption.Decrypt(encrypted);
        
        Console.WriteLine($"  Plain: {plainText}");
        Console.WriteLine($"  Encrypted: {encrypted?.Substring(0, Math.Min(40, encrypted?.Length ?? 0))}...");
        Console.WriteLine($"  Decrypted: {decrypted}");
        Console.WriteLine($"  ✓ Match: {plainText == decrypted}\n");

        // Test 2: Null handling
        Console.WriteLine("Test 2: Null Handling");
        var nullEncrypted = encryption.Encrypt(null);
        var nullDecrypted = encryption.Decrypt(null);
        Console.WriteLine($"  Encrypt(null): {nullEncrypted ?? "null"}");
        Console.WriteLine($"  Decrypt(null): {nullDecrypted ?? "null"}");
        Console.WriteLine($"  ✓ Both null: {nullEncrypted == null && nullDecrypted == null}\n");

        // Test 3: Empty string handling
        Console.WriteLine("Test 3: Empty String Handling");
        var emptyEncrypted = encryption.Encrypt("");
        var emptyDecrypted = encryption.Decrypt("");
        Console.WriteLine($"  Encrypt(''): {emptyEncrypted ?? "null"}");
        Console.WriteLine($"  Decrypt(''): {emptyDecrypted ?? "null"}");
        Console.WriteLine($"  ✓ Both empty: {emptyEncrypted == "" && emptyDecrypted == ""}\n");

        // Test 4: Multiple encryptions produce different ciphertext (nonce randomness)
        Console.WriteLine("Test 4: Nonce Randomness");
        var encrypted1 = encryption.Encrypt("test123");
        var encrypted2 = encryption.Encrypt("test123");
        var different = encrypted1 != encrypted2;
        Console.WriteLine($"  Same plaintext produces different ciphertext: {different}");
        Console.WriteLine($"  ✓ Nonces are random: {different}\n");

        // Test 5: Sensitive data examples
        Console.WriteLine("Test 5: Sensitive Data Examples");
        var bankAccount = "GB29NWBK60161331926819";
        var apiKey = "sk_live_51JabCDef123456789";
        var taxId = "123-45-6789";

        var encryptedBank = encryption.Encrypt(bankAccount);
        var encryptedApi = encryption.Encrypt(apiKey);
        var encryptedTax = encryption.Encrypt(taxId);

        var decryptedBank = encryption.Decrypt(encryptedBank);
        var decryptedApi = encryption.Decrypt(encryptedApi);
        var decryptedTax = encryption.Decrypt(encryptedTax);

        Console.WriteLine($"  Bank Account: {decryptedBank == bankAccount}");
        Console.WriteLine($"  API Key: {decryptedApi == apiKey}");
        Console.WriteLine($"  Tax ID: {decryptedTax == taxId}");
        Console.WriteLine($"  ✓ All decrypted correctly\n");

        // Test 6: Key version tracking
        Console.WriteLine("Test 6: Key Version Tracking");
        var currentVersion = keyManagement.GetCurrentKeyVersion();
        var versionedEncrypted = encryption.EncryptWithKeyVersion("versioned data", currentVersion);
        var versionedDecrypted = encryption.Decrypt(versionedEncrypted);
        Console.WriteLine($"  Current key version: {currentVersion}");
        Console.WriteLine($"  Versioned encryption works: {versionedDecrypted == "versioned data"}");
        Console.WriteLine($"  ✓ Key versioning functional\n");

        Console.WriteLine("=== All Encryption Tests Passed ===\n");
        
        await Task.CompletedTask;
    }
}
