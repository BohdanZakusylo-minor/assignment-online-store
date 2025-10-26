using Microsoft.EntityFrameworkCore;

namespace OrderManagerService.Models;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId);
            entity.Property(e => e.OrderId).ValueGeneratedOnAdd();
            entity.Property(e => e.ProductsId)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(int.Parse)
                          .ToList())
                .IsRequired();
            entity.Property(e => e.OrderDate).IsRequired();
            entity.Property(e => e.ShipmentDate).IsRequired(false);
            entity.Property(e => e.Destination).IsRequired().HasMaxLength(500);
        });
    }
}
