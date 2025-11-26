using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Domain.Entities; 
using Domain.Entities.Identity;
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Your Custom Entities
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Review> Reviews { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // This is crucial to ensure Identity models are configured correctly.
        base.OnModelCreating(builder);

        // === CONFIGURE RELATIONSHIPS ===

        // AppUser Relationships (Many are handled by IdentityDbContext)
        // 1-to-1 relationship between AppUser and Cart
        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Cart)
            .WithOne(c => c.User)
            .HasForeignKey<Cart>(c => c.UserId);

        // Category Self-Referencing Relationship (for parent/sub-categories)
        builder.Entity<Category>()
            .HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .IsRequired(false) // A category might not have a parent
            .OnDelete(DeleteBehavior.Restrict); // Prevent deleting a parent category if it has children

        // Order and Payment 1-to-1 Relationship
        builder.Entity<Order>()
            .HasOne(o => o.Payment)
            .WithOne(p => p.Order)
            .HasForeignKey<Payment>(p => p.OrderId);

        // Many-to-Many: Order and Product through OrderItem
        builder.Entity<OrderItem>()
            .HasKey(oi => new { oi.OrderId, oi.ProductId }); // Often a composite key is better for join tables

        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId);

        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany() // A product can be in many OrderItems, but we don't need a navigation property on Product
            .HasForeignKey(oi => oi.ProductId);

        // Many-to-Many: Cart and Product through CartItem
        builder.Entity<CartItem>()
            .HasKey(ci => new { ci.CartId, ci.ProductId }); // Composite primary key

        builder.Entity<CartItem>()
            .HasOne(ci => ci.Cart)
            .WithMany(c => c.CartItems)
            .HasForeignKey(ci => ci.CartId);

        builder.Entity<CartItem>()
            .HasOne(ci => ci.Product)
            .WithMany() // A product can be in many CartItems
            .HasForeignKey(ci => ci.ProductId);


        // === CONFIGURE DATA TYPES & PRECISION ===
        // Best practice for currency properties to avoid precision issues.
        builder.Entity<Product>()
            .Property(p => p.Price)
            .HasColumnType("decimal(18,2)");

        builder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Entity<OrderItem>()
            .Property(oi => oi.PriceAtPurchase)
            .HasColumnType("decimal(18,2)");

        builder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasColumnType("decimal(18,2)");
    }
}