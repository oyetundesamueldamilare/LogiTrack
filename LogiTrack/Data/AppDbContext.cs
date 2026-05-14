using LogiTrack.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LogiTrack.Data
{
    // Inherit from IdentityDbContext<AppUser> so ASP.NET Identity tables
    // (Users, Roles, Claims, etc.) are included alongside your custom entities.
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        // Constructor passes options to the base DbContext
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // DbSets represent tables in the database
        public DbSet<Order> Orders { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        // Configure relationships and column types
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // One OrderItem belongs to one Order, 
            // and an Order can have many OrderItems
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId);

            // One OrderItem belongs to one InventoryItem,
            // but InventoryItem doesn’t need a navigation property back
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.InventoryItem)
                .WithMany()
                .HasForeignKey(oi => oi.ItemId);

            // Configure decimal precision for prices
            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.UnitPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasColumnType("decimal(18,2)");

            // Always call base.OnModelCreating so Identity can configure its tables
            base.OnModelCreating(modelBuilder);
        }
    }
}
