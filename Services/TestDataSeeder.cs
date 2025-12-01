using MercatoApp.Data;
using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace MercatoApp.Services;

/// <summary>
/// Service for seeding test data into the database.
/// Used for development and testing purposes only.
/// </summary>
public static class TestDataSeeder
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int Pbkdf2Iterations = 100000;

    /// <summary>
    /// Seeds test data into the database.
    /// </summary>
    public static async Task SeedTestDataAsync(ApplicationDbContext context)
    {
        // Check if data already exists
        if (await context.Users.AnyAsync())
        {
            return; // Database already has data
        }

        // Create a test seller user
        var sellerUser = new User
        {
            Email = "seller@test.com",
            PasswordHash = HashPassword("Test123!"),
            FirstName = "Test",
            LastName = "Seller",
            UserType = UserType.Seller,
            Status = AccountStatus.Active,
            AcceptedTerms = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(sellerUser);
        await context.SaveChangesAsync();

        // Create a test store
        var store = new Store
        {
            UserId = sellerUser.Id,
            StoreName = "Test Electronics Store",
            Slug = "test-electronics",
            Category = "Electronics",
            Description = "A test store selling electronic products",
            Status = StoreStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Stores.Add(store);
        await context.SaveChangesAsync();

        // Create test products
        var products = new[]
        {
            new Product
            {
                StoreId = store.Id,
                Title = "Wireless Bluetooth Headphones",
                Description = "High-quality wireless headphones with noise cancellation",
                Price = 79.99m,
                Stock = 50,
                Category = "Electronics",
                Status = ProductStatus.Active,
                Condition = ProductCondition.New,
                ImageUrls = "https://via.placeholder.com/300x300?text=Headphones",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                StoreId = store.Id,
                Title = "Smart Watch Pro",
                Description = "Advanced smartwatch with fitness tracking and heart rate monitor",
                Price = 299.99m,
                Stock = 30,
                Category = "Electronics",
                Status = ProductStatus.Active,
                Condition = ProductCondition.New,
                ImageUrls = "https://via.placeholder.com/300x300?text=SmartWatch",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                StoreId = store.Id,
                Title = "Portable Bluetooth Speaker",
                Description = "Waterproof portable speaker with 20-hour battery life",
                Price = 49.99m,
                Stock = 100,
                Category = "Electronics",
                Status = ProductStatus.Active,
                Condition = ProductCondition.New,
                ImageUrls = "https://via.placeholder.com/300x300?text=Speaker",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                StoreId = store.Id,
                Title = "USB-C Fast Charging Cable",
                Description = "Durable 6ft USB-C cable with fast charging support",
                Price = 12.99m,
                Stock = 200,
                Category = "Electronics",
                Status = ProductStatus.Active,
                Condition = ProductCondition.New,
                ImageUrls = "https://via.placeholder.com/300x300?text=Cable",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                StoreId = store.Id,
                Title = "Laptop Stand Aluminum",
                Description = "Ergonomic laptop stand with adjustable height",
                Price = 34.99m,
                Stock = 75,
                Category = "Electronics",
                Status = ProductStatus.Active,
                Condition = ProductCondition.New,
                ImageUrls = "https://via.placeholder.com/300x300?text=LaptopStand",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    private static string HashPassword(string password)
    {
        // Generate a random salt
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);

        // Hash the password using PBKDF2
        var hashed = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: Pbkdf2Iterations,
            numBytesRequested: HashSizeBytes);

        // Combine salt and hash for storage
        var hashBytes = new byte[salt.Length + hashed.Length];
        Array.Copy(salt, 0, hashBytes, 0, salt.Length);
        Array.Copy(hashed, 0, hashBytes, salt.Length, hashed.Length);

        return Convert.ToBase64String(hashBytes);
    }
}
