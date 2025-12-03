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
            KycStatus = KycStatus.Approved,
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

        // Create test categories
        var electronicsCategory = new Category
        {
            Name = "Electronics",
            Slug = "electronics",
            Description = "Electronic devices and accessories",
            DisplayOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Categories.Add(electronicsCategory);

        var fashionCategory = new Category
        {
            Name = "Fashion",
            Slug = "fashion",
            Description = "Clothing, shoes, and accessories",
            DisplayOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Categories.Add(fashionCategory);

        var homeGardenCategory = new Category
        {
            Name = "Home & Garden",
            Slug = "home-garden",
            Description = "Home decor, furniture, and gardening supplies",
            DisplayOrder = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Categories.Add(homeGardenCategory);

        await context.SaveChangesAsync();

        // Create subcategories
        var computersCategory = new Category
        {
            Name = "Computers & Laptops",
            Slug = "computers-laptops",
            Description = "Desktop computers, laptops, and accessories",
            ParentCategoryId = electronicsCategory.Id,
            DisplayOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Categories.Add(computersCategory);

        var audioCategory = new Category
        {
            Name = "Audio & Headphones",
            Slug = "audio-headphones",
            Description = "Headphones, speakers, and audio equipment",
            ParentCategoryId = electronicsCategory.Id,
            DisplayOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Categories.Add(audioCategory);

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
                CategoryId = audioCategory.Id,
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
                CategoryId = audioCategory.Id,
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

        // Create a second test seller user and store
        var sellerUser2 = new User
        {
            Email = "seller2@test.com",
            PasswordHash = HashPassword("Test123!"),
            FirstName = "Second",
            LastName = "Seller",
            UserType = UserType.Seller,
            Status = AccountStatus.Active,
            KycStatus = KycStatus.Approved,
            AcceptedTerms = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(sellerUser2);
        await context.SaveChangesAsync();

        var store2 = new Store
        {
            UserId = sellerUser2.Id,
            StoreName = "Fashion Boutique",
            Slug = "fashion-boutique",
            Category = "Fashion",
            Description = "Trendy fashion items",
            Status = StoreStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Stores.Add(store2);
        await context.SaveChangesAsync();

        // Create products for the second seller
        var products2 = new[]
        {
            new Product
            {
                StoreId = store2.Id,
                Title = "Leather Wallet",
                Description = "Genuine leather bifold wallet with RFID protection",
                Price = 45.00m,
                Stock = 40,
                Category = "Fashion",
                Status = ProductStatus.Active,
                Condition = ProductCondition.New,
                ImageUrls = "https://via.placeholder.com/300x300?text=Wallet",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                StoreId = store2.Id,
                Title = "Designer Sunglasses",
                Description = "UV protection polarized sunglasses",
                Price = 89.99m,
                Stock = 25,
                Category = "Fashion",
                Status = ProductStatus.Active,
                Condition = ProductCondition.New,
                ImageUrls = "https://via.placeholder.com/300x300?text=Sunglasses",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Products.AddRange(products2);
        await context.SaveChangesAsync();

        // Create a test buyer user
        var buyerUser = new User
        {
            Email = "buyer@test.com",
            PasswordHash = HashPassword("Test123!"),
            FirstName = "Test",
            LastName = "Buyer",
            UserType = UserType.Buyer,
            Status = AccountStatus.Active,
            AcceptedTerms = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(buyerUser);
        await context.SaveChangesAsync();

        // Create a test admin user
        var adminUser = new User
        {
            Email = "admin@test.com",
            PasswordHash = HashPassword("Test123!"),
            FirstName = "Admin",
            LastName = "User",
            UserType = UserType.Admin,
            Status = AccountStatus.Active,
            AcceptedTerms = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        // Create a test cart with items from both sellers
        var cart = new Cart
        {
            UserId = buyerUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Carts.Add(cart);
        await context.SaveChangesAsync();

        // Add cart items from first seller
        var cartItems = new[]
        {
            new CartItem
            {
                CartId = cart.Id,
                ProductId = products[0].Id, // Wireless Headphones
                Quantity = 2,
                PriceAtAdd = products[0].Price,
                AddedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new CartItem
            {
                CartId = cart.Id,
                ProductId = products[2].Id, // Bluetooth Speaker
                Quantity = 1,
                PriceAtAdd = products[2].Price,
                AddedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            // Add items from second seller
            new CartItem
            {
                CartId = cart.Id,
                ProductId = products2[0].Id, // Leather Wallet
                Quantity = 1,
                PriceAtAdd = products2[0].Price,
                AddedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.CartItems.AddRange(cartItems);
        await context.SaveChangesAsync();

        // Create shipping rules for both stores
        var shippingRules = new[]
        {
            new ShippingRule
            {
                StoreId = store.Id,
                Name = "Standard Shipping",
                BaseCost = 5.99m,
                AdditionalItemCost = 1.00m,
                FreeShippingThreshold = 100.00m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ShippingRule
            {
                StoreId = store2.Id,
                Name = "Standard Shipping",
                BaseCost = 4.99m,
                AdditionalItemCost = 0.50m,
                FreeShippingThreshold = 75.00m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.ShippingRules.AddRange(shippingRules);
        await context.SaveChangesAsync();

        // Create shipping methods
        var shippingMethod1 = new ShippingMethod
        {
            StoreId = store.Id,
            Name = "Standard Shipping",
            EstimatedDelivery = "3-5 business days",
            BaseCost = 5.99m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var shippingMethod2 = new ShippingMethod
        {
            StoreId = store2.Id,
            Name = "Express Shipping",
            EstimatedDelivery = "1-2 business days",
            BaseCost = 12.99m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.ShippingMethods.AddRange(shippingMethod1, shippingMethod2);
        await context.SaveChangesAsync();

        // Create a test delivery address
        var deliveryAddress = new Address
        {
            UserId = buyerUser.Id,
            FullName = "Test Buyer",
            PhoneNumber = "+1-555-123-4567",
            AddressLine1 = "123 Main Street",
            AddressLine2 = "Apt 4B",
            City = "San Francisco",
            StateProvince = "CA",
            PostalCode = "94102",
            CountryCode = "US",
            DeliveryInstructions = "Please leave package at the front door. Ring doorbell if signature required.",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Addresses.Add(deliveryAddress);
        await context.SaveChangesAsync();

        // Create a test payment method
        var paymentMethod = new PaymentMethod
        {
            Name = "Credit Card",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.PaymentMethods.Add(paymentMethod);
        await context.SaveChangesAsync();

        // Create a test order with sub-orders
        var order = new Order
        {
            OrderNumber = "ORD-20241202-00001",
            UserId = buyerUser.Id,
            DeliveryAddressId = deliveryAddress.Id,
            Status = OrderStatus.Paid,
            Subtotal = 234.98m,
            ShippingCost = 18.98m,
            TaxAmount = 23.50m,
            TotalAmount = 277.46m,
            PaymentMethodId = paymentMethod.Id,
            PaymentStatus = PaymentStatus.Completed,
            OrderedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Create seller sub-orders
        var subOrder1 = new SellerSubOrder
        {
            ParentOrderId = order.Id,
            StoreId = store.Id,
            SubOrderNumber = "ORD-20241202-00001-1",
            Status = OrderStatus.Preparing,
            Subtotal = 189.98m,
            ShippingCost = 5.99m,
            TotalAmount = 195.97m,
            ShippingMethodId = shippingMethod1.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };

        var subOrder2 = new SellerSubOrder
        {
            ParentOrderId = order.Id,
            StoreId = store2.Id,
            SubOrderNumber = "ORD-20241202-00001-2",
            Status = OrderStatus.Delivered,  // Changed to Delivered for SLA testing
            TrackingNumber = "1Z999AA10123456784",
            CarrierName = "UPS",
            TrackingUrl = "https://www.ups.com/track?tracknum=1Z999AA10123456784",
            Subtotal = 45.00m,
            ShippingCost = 12.99m,
            TotalAmount = 57.99m,
            ShippingMethodId = shippingMethod2.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-3)
        };

        context.SellerSubOrders.AddRange(subOrder1, subOrder2);
        await context.SaveChangesAsync();

        // Create order items
        var orderItems = new[]
        {
            new OrderItem
            {
                OrderId = order.Id,
                SellerSubOrderId = subOrder1.Id,
                StoreId = store.Id,
                ProductId = products[0].Id,
                ProductTitle = "Wireless Bluetooth Headphones",
                VariantDescription = null,
                Quantity = 2,
                UnitPrice = 79.99m,
                Subtotal = 159.98m,
                TaxAmount = 16.00m
            },
            new OrderItem
            {
                OrderId = order.Id,
                SellerSubOrderId = subOrder1.Id,
                StoreId = store.Id,
                ProductId = products[2].Id,
                ProductTitle = "Portable Bluetooth Speaker",
                VariantDescription = "Color: Black",
                Quantity = 1,
                UnitPrice = 30.00m,
                Subtotal = 30.00m,
                TaxAmount = 3.00m
            },
            new OrderItem
            {
                OrderId = order.Id,
                SellerSubOrderId = subOrder2.Id,
                StoreId = store2.Id,
                ProductId = products2[0].Id,
                ProductTitle = "Leather Wallet",
                VariantDescription = null,
                Quantity = 1,
                UnitPrice = 45.00m,
                Subtotal = 45.00m,
                TaxAmount = 4.50m
            }
        };

        context.OrderItems.AddRange(orderItems);
        await context.SaveChangesAsync();

        // Create status history for sub-orders
        var statusHistories = new[]
        {
            // SubOrder1 history
            new OrderStatusHistory
            {
                SellerSubOrderId = subOrder1.Id,
                PreviousStatus = null,
                NewStatus = OrderStatus.New,
                Notes = "Order created",
                ChangedAt = DateTime.UtcNow.AddDays(-2)
            },
            new OrderStatusHistory
            {
                SellerSubOrderId = subOrder1.Id,
                PreviousStatus = OrderStatus.New,
                NewStatus = OrderStatus.Paid,
                Notes = "Payment completed",
                ChangedAt = DateTime.UtcNow.AddDays(-2).AddMinutes(5)
            },
            new OrderStatusHistory
            {
                SellerSubOrderId = subOrder1.Id,
                PreviousStatus = OrderStatus.Paid,
                NewStatus = OrderStatus.Preparing,
                Notes = null,
                ChangedAt = DateTime.UtcNow.AddHours(-1)
            },
            // SubOrder2 history
            new OrderStatusHistory
            {
                SellerSubOrderId = subOrder2.Id,
                PreviousStatus = null,
                NewStatus = OrderStatus.New,
                Notes = "Order created",
                ChangedAt = DateTime.UtcNow.AddDays(-2)
            },
            new OrderStatusHistory
            {
                SellerSubOrderId = subOrder2.Id,
                PreviousStatus = OrderStatus.New,
                NewStatus = OrderStatus.Paid,
                Notes = "Payment completed",
                ChangedAt = DateTime.UtcNow.AddDays(-2).AddMinutes(5)
            },
            new OrderStatusHistory
            {
                SellerSubOrderId = subOrder2.Id,
                PreviousStatus = OrderStatus.Paid,
                NewStatus = OrderStatus.Preparing,
                Notes = null,
                ChangedAt = DateTime.UtcNow.AddDays(-1)
            },
            new OrderStatusHistory
            {
                SellerSubOrderId = subOrder2.Id,
                PreviousStatus = OrderStatus.Preparing,
                NewStatus = OrderStatus.Shipped,
                Notes = "Tracking: 1Z999AA10123456784 via UPS",
                ChangedAt = DateTime.UtcNow.AddHours(-3)
            },
            new OrderStatusHistory
            {
                SellerSubOrderId = subOrder2.Id,
                PreviousStatus = OrderStatus.Shipped,
                NewStatus = OrderStatus.Delivered,
                Notes = "Package delivered successfully",
                ChangedAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        context.OrderStatusHistories.AddRange(statusHistories);
        await context.SaveChangesAsync();

        // Seed test promo codes
        var promoCodes = new[]
        {
            // Platform-wide percentage discount
            new PromoCode
            {
                Code = "SAVE20",
                Scope = PromoCodeScope.Platform,
                DiscountType = PromoCodeDiscountType.Percentage,
                DiscountValue = 20m,
                MinimumOrderSubtotal = 50m,
                MaximumDiscountAmount = 50m,
                StartDate = DateTime.UtcNow.AddDays(-30),
                ExpirationDate = DateTime.UtcNow.AddDays(30),
                MaximumUsageCount = 100,
                CurrentUsageCount = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            // Platform-wide fixed discount
            new PromoCode
            {
                Code = "WELCOME10",
                Scope = PromoCodeScope.Platform,
                DiscountType = PromoCodeDiscountType.FixedAmount,
                DiscountValue = 10m,
                MinimumOrderSubtotal = 30m,
                StartDate = DateTime.UtcNow.AddDays(-60),
                ExpirationDate = DateTime.UtcNow.AddDays(60),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            // Seller-specific promo code for store1
            new PromoCode
            {
                Code = "ELECTRONICS15",
                Scope = PromoCodeScope.Seller,
                StoreId = store.Id,
                DiscountType = PromoCodeDiscountType.Percentage,
                DiscountValue = 15m,
                MinimumOrderSubtotal = 100m,
                MaximumDiscountAmount = 30m,
                StartDate = DateTime.UtcNow.AddDays(-15),
                ExpirationDate = DateTime.UtcNow.AddDays(45),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            // Expired promo code for testing
            new PromoCode
            {
                Code = "EXPIRED",
                Scope = PromoCodeScope.Platform,
                DiscountType = PromoCodeDiscountType.Percentage,
                DiscountValue = 50m,
                ExpirationDate = DateTime.UtcNow.AddDays(-1),
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-90)
            }
        };

        context.PromoCodes.AddRange(promoCodes);
        await context.SaveChangesAsync();

        // Seed shipping providers
        var shippingProviders = new[]
        {
            new ShippingProvider
            {
                ProviderId = "mock_standard",
                Name = "Mock Standard Shipping",
                Description = "Simulated standard shipping provider for development and testing",
                IsActive = true,
                SupportsAutomation = true,
                SupportsWebhooks = false,
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow
            },
            new ShippingProvider
            {
                ProviderId = "mock_express",
                Name = "Mock Express Shipping",
                Description = "Simulated express shipping provider for development and testing",
                IsActive = true,
                SupportsAutomation = true,
                SupportsWebhooks = false,
                DisplayOrder = 2,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.ShippingProviders.AddRange(shippingProviders);
        await context.SaveChangesAsync();

        // Configure mock_standard provider for the test store
        var providerConfig = new ShippingProviderConfig
        {
            StoreId = store.Id,
            ShippingProviderId = shippingProviders[0].Id,
            IsEnabled = true,
            AccountNumber = "TEST-ACCOUNT-001",
            AutoCreateShipments = true,
            AutoSendTrackingUpdates = true,
            CreatedAt = DateTime.UtcNow
        };

        context.ShippingProviderConfigs.Add(providerConfig);
        await context.SaveChangesAsync();

        // Add seller ratings for testing the rating display feature
        // These ratings demonstrate the seller rating functionality on the store page
        // Note: Ratings can only be added for delivered sub-orders per business rules
        // subOrder2 (Fashion Boutique) has Status = OrderStatus.Delivered (see line 386)
        var sellerRating = new SellerRating
        {
            StoreId = store2.Id, // Fashion Boutique
            UserId = buyerUser.Id,
            SellerSubOrderId = subOrder2.Id, // This sub-order is in Delivered status
            Rating = 5,
            CreatedAt = DateTime.UtcNow.AddDays(-1) // Backdated to simulate a rating submitted yesterday
        };

        context.SellerRatings.Add(sellerRating);
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
