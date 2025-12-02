using MercatoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MercatoApp.Data;

/// <summary>
/// Database context for the Mercato application.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the users table.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user sessions table.
    /// Sessions are stored in the database to support horizontal scaling.
    /// </summary>
    public DbSet<UserSession> UserSessions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the login events table for security auditing.
    /// Login events track authentication attempts and support security alerting.
    /// </summary>
    public DbSet<LoginEvent> LoginEvents { get; set; } = null!;

    /// <summary>
    /// Gets or sets the stores table.
    /// </summary>
    public DbSet<Store> Stores { get; set; } = null!;

    /// <summary>
    /// Gets or sets the seller onboarding drafts table.
    /// </summary>
    public DbSet<SellerOnboardingDraft> SellerOnboardingDrafts { get; set; } = null!;

    /// <summary>
    /// Gets or sets the seller verifications table.
    /// </summary>
    public DbSet<SellerVerification> SellerVerifications { get; set; } = null!;

    /// <summary>
    /// Gets or sets the payout methods table.
    /// </summary>
    public DbSet<PayoutMethod> PayoutMethods { get; set; } = null!;

    /// <summary>
    /// Gets or sets the store user roles table.
    /// Maps users to stores with their assigned internal role.
    /// </summary>
    public DbSet<StoreUserRole> StoreUserRoles { get; set; } = null!;

    /// <summary>
    /// Gets or sets the store user invitations table.
    /// Tracks pending invitations for internal users to join a store.
    /// </summary>
    public DbSet<StoreUserInvitation> StoreUserInvitations { get; set; } = null!;

    /// <summary>
    /// Gets or sets the products table.
    /// </summary>
    public DbSet<Product> Products { get; set; } = null!;

    /// <summary>
    /// Gets or sets the categories table.
    /// </summary>
    public DbSet<Category> Categories { get; set; } = null!;

    /// <summary>
    /// Gets or sets the product images table.
    /// </summary>
    public DbSet<ProductImage> ProductImages { get; set; } = null!;

    /// <summary>
    /// Gets or sets the product import jobs table.
    /// </summary>
    public DbSet<ProductImportJob> ProductImportJobs { get; set; } = null!;

    /// <summary>
    /// Gets or sets the product import results table.
    /// </summary>
    public DbSet<ProductImportResult> ProductImportResults { get; set; } = null!;

    /// <summary>
    /// Gets or sets the product variant attributes table.
    /// </summary>
    public DbSet<ProductVariantAttribute> ProductVariantAttributes { get; set; } = null!;

    /// <summary>
    /// Gets or sets the product variant attribute values table.
    /// </summary>
    public DbSet<ProductVariantAttributeValue> ProductVariantAttributeValues { get; set; } = null!;

    /// <summary>
    /// Gets or sets the product variants table.
    /// </summary>
    public DbSet<ProductVariant> ProductVariants { get; set; } = null!;

    /// <summary>
    /// Gets or sets the product variant options table.
    /// </summary>
    public DbSet<ProductVariantOption> ProductVariantOptions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the carts table.
    /// </summary>
    public DbSet<Cart> Carts { get; set; } = null!;

    /// <summary>
    /// Gets or sets the cart items table.
    /// </summary>
    public DbSet<CartItem> CartItems { get; set; } = null!;

    /// <summary>
    /// Gets or sets the shipping rules table.
    /// </summary>
    public DbSet<ShippingRule> ShippingRules { get; set; } = null!;

    /// <summary>
    /// Gets or sets the commission configurations table.
    /// </summary>
    public DbSet<CommissionConfig> CommissionConfigs { get; set; } = null!;

    /// <summary>
    /// Gets or sets the addresses table.
    /// </summary>
    public DbSet<Address> Addresses { get; set; } = null!;

    /// <summary>
    /// Gets or sets the orders table.
    /// </summary>
    public DbSet<Order> Orders { get; set; } = null!;

    /// <summary>
    /// Gets or sets the order items table.
    /// </summary>
    public DbSet<OrderItem> OrderItems { get; set; } = null!;

    /// <summary>
    /// Gets or sets the seller sub-orders table.
    /// </summary>
    public DbSet<SellerSubOrder> SellerSubOrders { get; set; } = null!;

    /// <summary>
    /// Gets or sets the shipping methods table.
    /// </summary>
    public DbSet<ShippingMethod> ShippingMethods { get; set; } = null!;

    /// <summary>
    /// Gets or sets the order shipping methods table.
    /// </summary>
    public DbSet<OrderShippingMethod> OrderShippingMethods { get; set; } = null!;

    /// <summary>
    /// Gets or sets the payment methods table.
    /// </summary>
    public DbSet<PaymentMethod> PaymentMethods { get; set; } = null!;

    /// <summary>
    /// Gets or sets the payment transactions table.
    /// </summary>
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the escrow transactions table.
    /// </summary>
    public DbSet<EscrowTransaction> EscrowTransactions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the promo codes table.
    /// </summary>
    public DbSet<PromoCode> PromoCodes { get; set; } = null!;

    /// <summary>
    /// Gets or sets the order status history table.
    /// </summary>
    public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; } = null!;

    /// <summary>
    /// Gets or sets the return requests table.
    /// </summary>
    public DbSet<ReturnRequest> ReturnRequests { get; set; } = null!;

    /// <summary>
    /// Gets or sets the return request items table.
    /// </summary>
    public DbSet<ReturnRequestItem> ReturnRequestItems { get; set; } = null!;

    /// <summary>
    /// Gets or sets the commission transactions table.
    /// </summary>
    public DbSet<CommissionTransaction> CommissionTransactions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the payout schedules table.
    /// </summary>
    public DbSet<PayoutSchedule> PayoutSchedules { get; set; } = null!;

    /// <summary>
    /// Gets or sets the payouts table.
    /// </summary>
    public DbSet<Payout> Payouts { get; set; } = null!;

    /// <summary>
    /// Gets or sets the settlements table.
    /// </summary>
    public DbSet<Settlement> Settlements { get; set; } = null!;

    /// <summary>
    /// Gets or sets the settlement items table.
    /// </summary>
    public DbSet<SettlementItem> SettlementItems { get; set; } = null!;

    /// <summary>
    /// Gets or sets the settlement adjustments table.
    /// </summary>
    public DbSet<SettlementAdjustment> SettlementAdjustments { get; set; } = null!;

    /// <summary>
    /// Gets or sets the settlement configuration table.
    /// </summary>
    public DbSet<SettlementConfig> SettlementConfigs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            // Index on token for fast lookups during validation
            entity.HasIndex(e => e.Token).IsUnique();
            
            // Index on user ID for invalidating all user sessions
            entity.HasIndex(e => e.UserId);
            
            // Index for cleanup of expired sessions
            entity.HasIndex(e => new { e.IsValid, e.ExpiresAt });

            // Configure relationship with User
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LoginEvent>(entity =>
        {
            // Index on user ID for querying user's login history
            entity.HasIndex(e => e.UserId);
            
            // Index on creation time for retention cleanup and time-based queries
            entity.HasIndex(e => e.CreatedAt);
            
            // Composite index for querying user's recent login events
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            
            // Index for security alert queries
            entity.HasIndex(e => new { e.UserId, e.IsSuccessful, e.CreatedAt });

            // Configure optional relationship with User
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Store>(entity =>
        {
            // Index on user ID for fast lookups
            entity.HasIndex(e => e.UserId).IsUnique();

            // Index on store name for uniqueness
            entity.HasIndex(e => e.StoreName).IsUnique();

            // Index on slug for SEO-friendly URL lookups
            entity.HasIndex(e => e.Slug).IsUnique();

            // Configure relationship with User
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure decimal precision for commission overrides
            entity.Property(e => e.CommissionPercentageOverride)
                .HasPrecision(5, 2);

            entity.Property(e => e.FixedCommissionAmountOverride)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<SellerOnboardingDraft>(entity =>
        {
            // Index on user ID for fast lookups (one draft per user)
            entity.HasIndex(e => e.UserId).IsUnique();

            // Configure relationship with User
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SellerVerification>(entity =>
        {
            // Index on user ID for fast lookups (one verification per user)
            entity.HasIndex(e => e.UserId).IsUnique();

            // Configure relationship with User
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PayoutMethod>(entity =>
        {
            // Index on store ID for fast lookups
            entity.HasIndex(e => e.StoreId);

            // Composite index for finding default payout method
            entity.HasIndex(e => new { e.StoreId, e.IsDefault });

            // Configure relationship with Store
            entity.HasOne(e => e.Store)
                .WithMany()
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoreUserRole>(entity =>
        {
            // Composite unique index - one role per user per store
            entity.HasIndex(e => new { e.StoreId, e.UserId }).IsUnique();

            // Index for finding all users in a store
            entity.HasIndex(e => e.StoreId);

            // Index for finding all stores a user belongs to
            entity.HasIndex(e => e.UserId);

            // Configure relationship with Store
            entity.HasOne(e => e.Store)
                .WithMany()
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relationship with User
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relationship with AssignedByUser (optional)
            entity.HasOne(e => e.AssignedByUser)
                .WithMany()
                .HasForeignKey(e => e.AssignedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StoreUserInvitation>(entity =>
        {
            // Index on invitation token for fast lookups
            entity.HasIndex(e => e.InvitationToken).IsUnique();

            // Index for finding pending invitations by email
            entity.HasIndex(e => new { e.Email, e.Status });

            // Index for finding invitations for a store
            entity.HasIndex(e => e.StoreId);

            // Configure relationship with Store
            entity.HasOne(e => e.Store)
                .WithMany()
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relationship with InvitedByUser
            entity.HasOne(e => e.InvitedByUser)
                .WithMany()
                .HasForeignKey(e => e.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relationship with AcceptedByUser (optional)
            entity.HasOne(e => e.AcceptedByUser)
                .WithMany()
                .HasForeignKey(e => e.AcceptedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            // Index on store ID for finding all products in a store
            entity.HasIndex(e => e.StoreId);

            // Composite index for finding products by store and status
            entity.HasIndex(e => new { e.StoreId, e.Status });

            // Index on CategoryId for category-based queries
            entity.HasIndex(e => e.CategoryId);

            // Index on Condition for filtering products by condition
            entity.HasIndex(e => e.Condition);

            // Composite unique index on StoreId and SKU (SKU must be unique within a store)
            // Note: In-memory database doesn't support filtered indexes
            // Null SKUs are allowed and don't participate in uniqueness check
            // This is enforced programmatically in the service layer
            entity.HasIndex(e => new { e.StoreId, e.Sku });

            // Configure relationship with Store
            entity.HasOne(e => e.Store)
                .WithMany(s => s.Products)
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relationship with Category (optional)
            entity.HasOne(e => e.CategoryEntity)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Price precision
            entity.Property(e => e.Price)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            // Index on parent category ID for tree queries
            entity.HasIndex(e => e.ParentCategoryId);

            // Index on IsActive for filtering active categories
            entity.HasIndex(e => e.IsActive);

            // Composite index for ordering categories within a parent
            entity.HasIndex(e => new { e.ParentCategoryId, e.DisplayOrder });

            // Configure self-referencing relationship
            entity.HasOne(e => e.ParentCategory)
                .WithMany(e => e.ChildCategories)
                .HasForeignKey(e => e.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure decimal precision for commission overrides
            entity.Property(e => e.CommissionPercentageOverride)
                .HasPrecision(5, 2);

            entity.Property(e => e.FixedCommissionAmountOverride)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            // Index on product ID for finding all images for a product
            entity.HasIndex(e => e.ProductId);

            // Composite index for finding the main image of a product
            entity.HasIndex(e => new { e.ProductId, e.IsMain });

            // Composite index for ordering images within a product
            entity.HasIndex(e => new { e.ProductId, e.DisplayOrder });

            // Index on variant ID for finding images specific to a variant
            entity.HasIndex(e => e.VariantId);

            // Configure relationship with Product
            entity.HasOne(e => e.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relationship with Variant (optional)
            // SetNull behavior: When variant is deleted, image remains for product but VariantId is cleared
            // This preserves images that may be useful for the main product listing
            entity.HasOne(e => e.Variant)
                .WithMany(v => v.Images)
                .HasForeignKey(e => e.VariantId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ProductImportJob>(entity =>
        {
            // Index on store ID for finding all import jobs for a store
            entity.HasIndex(e => e.StoreId);

            // Index on user ID for finding all import jobs by a user
            entity.HasIndex(e => e.UserId);

            // Index on status for filtering jobs by status
            entity.HasIndex(e => e.Status);

            // Composite index for ordering jobs by store and date
            entity.HasIndex(e => new { e.StoreId, e.CreatedAt });

            // Configure relationship with Store
            entity.HasOne(e => e.Store)
                .WithMany()
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relationship with User
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductImportResult>(entity =>
        {
            // Index on job ID for finding all results for a job
            entity.HasIndex(e => e.JobId);

            // Composite index for ordering results within a job
            entity.HasIndex(e => new { e.JobId, e.RowNumber });

            // Composite index for finding failed results
            entity.HasIndex(e => new { e.JobId, e.Success });

            // Configure relationship with Job
            entity.HasOne(e => e.Job)
                .WithMany(j => j.Results)
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductVariantAttribute>(entity =>
        {
            // Index on product ID for finding all attributes for a product
            entity.HasIndex(e => e.ProductId);

            // Composite index for ordering attributes within a product
            entity.HasIndex(e => new { e.ProductId, e.DisplayOrder });

            // Configure relationship with Product
            entity.HasOne(e => e.Product)
                .WithMany(p => p.VariantAttributes)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductVariantAttributeValue>(entity =>
        {
            // Index on variant attribute ID for finding all values for an attribute
            entity.HasIndex(e => e.VariantAttributeId);

            // Composite index for ordering values within an attribute
            entity.HasIndex(e => new { e.VariantAttributeId, e.DisplayOrder });

            // Configure relationship with VariantAttribute
            entity.HasOne(e => e.VariantAttribute)
                .WithMany(a => a.Values)
                .HasForeignKey(e => e.VariantAttributeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            // Index on product ID for finding all variants for a product
            entity.HasIndex(e => e.ProductId);

            // Index for finding enabled variants
            entity.HasIndex(e => new { e.ProductId, e.IsEnabled });

            // Composite unique index on ProductId and SKU (SKU must be unique within a product if set)
            entity.HasIndex(e => new { e.ProductId, e.Sku });

            // Configure relationship with Product
            entity.HasOne(e => e.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure PriceOverride precision
            entity.Property(e => e.PriceOverride)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<ProductVariantOption>(entity =>
        {
            // Index on product variant ID for finding all options for a variant
            entity.HasIndex(e => e.ProductVariantId);

            // Composite unique index - one attribute value per variant
            entity.HasIndex(e => new { e.ProductVariantId, e.AttributeValueId }).IsUnique();

            // Configure relationship with ProductVariant
            entity.HasOne(e => e.ProductVariant)
                .WithMany(v => v.Options)
                .HasForeignKey(e => e.ProductVariantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relationship with AttributeValue
            // Restrict delete to prevent removing attribute values that are used by variants
            // Attribute values should only be deleted when the parent attribute is deleted
            entity.HasOne(e => e.AttributeValue)
                .WithMany(av => av.VariantOptions)
                .HasForeignKey(e => e.AttributeValueId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            // Index on user ID for finding cart by user
            entity.HasIndex(e => e.UserId);

            // Index on session ID for finding cart by anonymous session
            entity.HasIndex(e => e.SessionId);

            // Configure optional relationship with User
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            // Index on cart ID for finding all items in a cart
            entity.HasIndex(e => e.CartId);

            // Index on product ID for finding carts containing a specific product
            entity.HasIndex(e => e.ProductId);

            // Composite index for finding a specific item in a cart
            entity.HasIndex(e => new { e.CartId, e.ProductId, e.ProductVariantId });

            // Configure relationship with Cart
            entity.HasOne(e => e.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(e => e.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relationship with Product
            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relationship with ProductVariant (optional)
            entity.HasOne(e => e.ProductVariant)
                .WithMany()
                .HasForeignKey(e => e.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure PriceAtAdd precision
            entity.Property(e => e.PriceAtAdd)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<ShippingRule>(entity =>
        {
            // Index on store ID for fast lookups
            entity.HasIndex(e => e.StoreId);

            // Configure relationship with Store
            entity.HasOne(e => e.Store)
                .WithMany()
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure decimal precision
            entity.Property(e => e.BaseCost)
                .HasPrecision(18, 2);

            entity.Property(e => e.AdditionalItemCost)
                .HasPrecision(18, 2);

            entity.Property(e => e.FreeShippingThreshold)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<Address>(entity =>
        {
            // Index on user ID for finding all addresses for a user
            entity.HasIndex(e => e.UserId);

            // Composite index for finding default address
            entity.HasIndex(e => new { e.UserId, e.IsDefault });

            // Configure optional relationship with User
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            // Index on order number for fast lookups
            entity.HasIndex(e => e.OrderNumber).IsUnique();

            // Index on user ID for finding all orders for a user
            entity.HasIndex(e => e.UserId);

            // Index on guest email for guest order lookups
            entity.HasIndex(e => e.GuestEmail);

            // Index on status for filtering orders
            entity.HasIndex(e => e.Status);

            // Composite index for ordering user's orders by date
            entity.HasIndex(e => new { e.UserId, e.OrderedAt });

            // Configure optional relationship with User
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure relationship with DeliveryAddress
            entity.HasOne(e => e.DeliveryAddress)
                .WithMany()
                .HasForeignKey(e => e.DeliveryAddressId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure decimal precision
            entity.Property(e => e.Subtotal)
                .HasPrecision(18, 2);

            entity.Property(e => e.ShippingCost)
                .HasPrecision(18, 2);

            entity.Property(e => e.TaxAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.TotalAmount)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            // Index on order ID for finding all items in an order
            entity.HasIndex(e => e.OrderId);

            // Index on store ID for finding items by seller
            entity.HasIndex(e => e.StoreId);

            // Index on product ID
            entity.HasIndex(e => e.ProductId);

            // Configure relationship with Order
            entity.HasOne(e => e.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relationship with Store
            entity.HasOne(e => e.Store)
                .WithMany()
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relationship with Product
            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relationship with ProductVariant (optional)
            entity.HasOne(e => e.ProductVariant)
                .WithMany()
                .HasForeignKey(e => e.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relationship with SellerSubOrder (optional)
            entity.HasOne(e => e.SellerSubOrder)
                .WithMany(s => s.Items)
                .HasForeignKey(e => e.SellerSubOrderId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure decimal precision
            entity.Property(e => e.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(e => e.Subtotal)
                .HasPrecision(18, 2);

            entity.Property(e => e.TaxAmount)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<SellerSubOrder>(entity =>
        {
            // Index on parent order ID for finding all sub-orders
            entity.HasIndex(e => e.ParentOrderId);

            // Index on store ID for finding sub-orders by seller
            entity.HasIndex(e => e.StoreId);

            // Index on sub-order number for fast lookups
            entity.HasIndex(e => e.SubOrderNumber).IsUnique();

            // Composite index for filtering seller's sub-orders by status
            entity.HasIndex(e => new { e.StoreId, e.Status });

            // Composite index for ordering seller's sub-orders by date
            entity.HasIndex(e => new { e.StoreId, e.CreatedAt });

            // Configure relationship with Parent Order
            entity.HasOne(e => e.ParentOrder)
                .WithMany(o => o.SubOrders)
                .HasForeignKey(e => e.ParentOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relationship with Store
            entity.HasOne(e => e.Store)
                .WithMany()
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relationship with ShippingMethod (optional)
            entity.HasOne(e => e.ShippingMethod)
                .WithMany()
                .HasForeignKey(e => e.ShippingMethodId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure decimal precision
            entity.Property(e => e.Subtotal)
                .HasPrecision(18, 2);

            entity.Property(e => e.ShippingCost)
                .HasPrecision(18, 2);

            entity.Property(e => e.TotalAmount)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<CommissionConfig>(entity =>
        {
            // Index for querying active configuration
            entity.HasIndex(e => e.IsActive);

            // Configure decimal precision
            entity.Property(e => e.CommissionPercentage)
                .HasPrecision(5, 2);

            entity.Property(e => e.FixedCommissionAmount)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<PromoCode>(entity =>
        {
            // Index on code for fast lookups (unique, case-insensitive)
            entity.HasIndex(e => e.Code).IsUnique();

            // Index on store ID for finding seller-specific promo codes
            entity.HasIndex(e => e.StoreId);

            // Composite index for finding active promo codes
            entity.HasIndex(e => new { e.IsActive, e.ExpirationDate });

            // Configure optional relationship with Store
            entity.HasOne(e => e.Store)
                .WithMany()
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure decimal precision
            entity.Property(e => e.DiscountValue)
                .HasPrecision(18, 2);

            entity.Property(e => e.MinimumOrderSubtotal)
                .HasPrecision(18, 2);

            entity.Property(e => e.MaximumDiscountAmount)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            // Index on sub-order ID for finding history
            entity.HasIndex(e => e.SellerSubOrderId);

            // Composite index for ordering history by date
            entity.HasIndex(e => new { e.SellerSubOrderId, e.ChangedAt });

            // Configure relationship with SellerSubOrder
            entity.HasOne(e => e.SellerSubOrder)
                .WithMany(s => s.StatusHistory)
                .HasForeignKey(e => e.SellerSubOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure optional relationship with User
            entity.HasOne(e => e.ChangedByUser)
                .WithMany()
                .HasForeignKey(e => e.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ReturnRequest>(entity =>
        {
            // Index on return number for fast lookups
            entity.HasIndex(e => e.ReturnNumber).IsUnique();

            // Index on sub-order ID for finding returns for a sub-order
            entity.HasIndex(e => e.SubOrderId);

            // Index on buyer ID for finding returns by buyer
            entity.HasIndex(e => e.BuyerId);

            // Index on status for filtering returns
            entity.HasIndex(e => e.Status);

            // Composite index for ordering returns by status and date
            entity.HasIndex(e => new { e.Status, e.RequestedAt });

            // Configure relationship with SellerSubOrder
            entity.HasOne(e => e.SubOrder)
                .WithMany()
                .HasForeignKey(e => e.SubOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relationship with Buyer
            entity.HasOne(e => e.Buyer)
                .WithMany()
                .HasForeignKey(e => e.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure decimal precision
            entity.Property(e => e.RefundAmount)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<ReturnRequestItem>(entity =>
        {
            // Index on return request ID for finding all items in a return
            entity.HasIndex(e => e.ReturnRequestId);

            // Index on order item ID for tracking returns for specific items
            entity.HasIndex(e => e.OrderItemId);

            // Configure relationship with ReturnRequest
            entity.HasOne(e => e.ReturnRequest)
                .WithMany(r => r.Items)
                .HasForeignKey(e => e.ReturnRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relationship with OrderItem
            entity.HasOne(e => e.OrderItem)
                .WithMany()
                .HasForeignKey(e => e.OrderItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure decimal precision
            entity.Property(e => e.RefundAmount)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<EscrowTransaction>(entity =>
        {
            // Index on payment transaction ID for finding all escrows from a payment
            entity.HasIndex(e => e.PaymentTransactionId);

            // Unique index on seller sub-order ID enforces one-to-one relationship
            // This ensures each sub-order has exactly one escrow allocation
            // Design decision: Each sub-order represents a single payment event
            entity.HasIndex(e => e.SellerSubOrderId).IsUnique();

            // Index on store ID for finding all escrows for a seller
            entity.HasIndex(e => e.StoreId);

            // Index on status for filtering escrows by status
            entity.HasIndex(e => e.Status);

            // Composite index for finding eligible payouts
            entity.HasIndex(e => new { e.Status, e.EligibleForPayoutAt });

            // Index on payout ID for finding all escrows in a payout
            entity.HasIndex(e => e.PayoutId);

            // Configure relationship with PaymentTransaction
            entity.HasOne(e => e.PaymentTransaction)
                .WithMany()
                .HasForeignKey(e => e.PaymentTransactionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relationship with SellerSubOrder
            entity.HasOne(e => e.SellerSubOrder)
                .WithMany()
                .HasForeignKey(e => e.SellerSubOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relationship with Store
            entity.HasOne(e => e.Store)
                .WithMany()
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relationship with Payout (optional)
            entity.HasOne(e => e.Payout)
                .WithMany(p => p.EscrowTransactions)
                .HasForeignKey(e => e.PayoutId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure decimal precision
            entity.Property(e => e.GrossAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.CommissionAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.NetAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.RefundedAmount)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<CommissionTransaction>(entity =>
        {
            // Index on escrow transaction ID for finding all commission transactions for an escrow
            entity.HasIndex(e => e.EscrowTransactionId);

            // Index on store ID for finding all commission transactions for a seller
            entity.HasIndex(e => e.StoreId);

            // Index on category ID for category-based reporting
            entity.HasIndex(e => e.CategoryId);

            // Index on transaction type for filtering by type
            entity.HasIndex(e => e.TransactionType);

            // Composite index for date-based queries
            entity.HasIndex(e => new { e.StoreId, e.CreatedAt });

            // Configure relationship with EscrowTransaction
            entity.HasOne(e => e.EscrowTransaction)
                .WithMany()
                .HasForeignKey(e => e.EscrowTransactionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relationship with Store
            entity.HasOne(e => e.Store)
                .WithMany()
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relationship with Category (optional)
            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure decimal precision
            entity.Property(e => e.GrossAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.CommissionPercentage)
                .HasPrecision(5, 2);

            entity.Property(e => e.FixedCommissionAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.CommissionAmount)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<PayoutSchedule>(entity =>
        {
            // Unique index on store ID - one schedule per store
            entity.HasIndex(e => e.StoreId).IsUnique();

            // Index for finding schedules due for processing
            entity.HasIndex(e => new { e.IsEnabled, e.NextPayoutDate });

            // Configure relationship with Store
            entity.HasOne(e => e.Store)
                .WithMany()
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure decimal precision
            entity.Property(e => e.MinimumPayoutThreshold)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<Payout>(entity =>
        {
            // Unique index on payout number
            entity.HasIndex(e => e.PayoutNumber).IsUnique();

            // Index on store ID for finding all payouts for a seller
            entity.HasIndex(e => e.StoreId);

            // Index on status for filtering payouts
            entity.HasIndex(e => e.Status);

            // Composite index for finding scheduled payouts
            entity.HasIndex(e => new { e.Status, e.ScheduledDate });

            // Composite index for finding failed payouts eligible for retry
            entity.HasIndex(e => new { e.Status, e.NextRetryDate });

            // Composite index for store's payout history
            entity.HasIndex(e => new { e.StoreId, e.CreatedAt });

            // Configure relationship with Store
            entity.HasOne(e => e.Store)
                .WithMany()
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relationship with PayoutMethod (optional)
            entity.HasOne(e => e.PayoutMethod)
                .WithMany()
                .HasForeignKey(e => e.PayoutMethodId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure relationship with PayoutSchedule (optional)
            entity.HasOne(e => e.PayoutSchedule)
                .WithMany()
                .HasForeignKey(e => e.PayoutScheduleId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure decimal precision
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2);
        });
    }
}
