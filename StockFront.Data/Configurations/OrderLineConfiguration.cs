using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFront.Data.Entities;

namespace StockFront.Data.Configurations;

public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.Quantity).IsRequired();

        b.Property(x => x.UnitPrice)
         .HasColumnType("decimal(12,2)");

        // Lines are deleted together with the order.
        b.HasOne(x => x.Order)
         .WithMany(o => o.Lines)
         .HasForeignKey(x => x.OrderId)
         .OnDelete(DeleteBehavior.Cascade);

        // A product can't be deleted while it is referenced by order lines.
        b.HasOne(x => x.Product)
         .WithMany()
         .HasForeignKey(x => x.ProductId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.OrderId);
        b.HasIndex(x => x.ProductId);
    }
}
