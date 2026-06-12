using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFront.Data.Entities;

namespace StockFront.Data.Configurations;

public sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.Quantity).IsRequired();
        b.Property(x => x.Reserved).IsRequired();

        // Available = Quantity - Reserved is computed in C#; it is not stored.
        b.Ignore(x => x.Available);

        // Optimistic-locking token. MySQL bumps it on every row change; the migration adds
        // DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6).
        b.Property(x => x.RowVersion)
         .HasColumnType("timestamp(6)")
         .ValueGeneratedOnAddOrUpdate()
         .IsConcurrencyToken();

        // One product — one stock record. Deleting the product removes its stock.
        b.HasOne(x => x.Product)
         .WithOne(p => p.Stock)
         .HasForeignKey<StockItem>(x => x.ProductId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.ProductId).IsUnique();
    }
}
