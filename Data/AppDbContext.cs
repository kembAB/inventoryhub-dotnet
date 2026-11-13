using InventoryHub.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryHub.Data;

/// <summary>
/// Entity Framework Core database context.
/// The provider (SQLite locally, PostgreSQL in Docker/cloud) is wired up
/// in Program.cs from configuration, so this class stays provider-neutral.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // A SKU identifies a product: enforce uniqueness at the database level.
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Sku)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasColumnType("decimal(18,2)");
    }

    /// <summary>Sample catalogue inserted on first run when the table is empty.</summary>
    public static Product[] SeedProducts => new[]
    {
        new Product { Id = 1, Name = "Wireless Mouse",      Sku = "WM-1001", Category = "Peripherals", Price = 24.99m,  StockQuantity = 140, CreatedAt = new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc) },
        new Product { Id = 2, Name = "Mechanical Keyboard", Sku = "KB-2001", Category = "Peripherals", Price = 89.50m,  StockQuantity = 64,  CreatedAt = new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc) },
        new Product { Id = 3, Name = "27-inch Monitor",     Sku = "MN-2701", Category = "Displays",    Price = 219.99m, StockQuantity = 23,  CreatedAt = new DateTime(2026, 1, 6, 9, 0, 0, DateTimeKind.Utc) },
        new Product { Id = 4, Name = "USB-C Docking Station", Sku = "DK-3001", Category = "Accessories", Price = 129.00m, StockQuantity = 4, CreatedAt = new DateTime(2026, 1, 6, 9, 0, 0, DateTimeKind.Utc) },
        new Product { Id = 5, Name = "Laptop Stand",        Sku = "LS-4001", Category = "Accessories", Price = 39.95m,  StockQuantity = 0,   CreatedAt = new DateTime(2026, 1, 7, 9, 0, 0, DateTimeKind.Utc) },
        new Product { Id = 6, Name = "Noise-Cancelling Headphones", Sku = "HP-5001", Category = "Audio", Price = 149.99m, StockQuantity = 37, CreatedAt = new DateTime(2026, 1, 7, 9, 0, 0, DateTimeKind.Utc) },
    };
}
