using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFront.Data.Entities;

namespace StockFront.Data.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.CustomerName)
         .IsRequired()
         .HasMaxLength(200);

        // Stored as a string ("New", "Reserved", ...) for readable rows and filters.
        b.Property(x => x.Status)
         .HasConversion<string>()
         .HasMaxLength(20)
         .IsRequired();

        b.Property(x => x.CreatedAt)
         .HasColumnType("datetime(6)")
         .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        // Lists and filters: "orders by status" and the frequent "new by date".
        b.HasIndex(x => x.Status);
        b.HasIndex(x => new { x.Status, x.CreatedAt });
    }
}
