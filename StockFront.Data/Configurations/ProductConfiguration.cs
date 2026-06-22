using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFront.Data.Entities;

namespace StockFront.Data.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.Sku)
         .IsRequired()
         .HasMaxLength(64);

        b.Property(x => x.Name)
         .IsRequired()
         .HasMaxLength(200);

        b.Property(x => x.Price)
         .HasColumnType("decimal(12,2)");

        // Set by the database on insert, so "Новинка" works without the app having to stamp the time.
        b.Property(x => x.CreatedAt)
         .HasColumnType("datetime(6)")
         .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        // Reorder-point inputs for the Days-of-Supply status. Sensible defaults so new rows are valid.
        b.Property(x => x.LeadTimeDays).HasDefaultValue(7);
        b.Property(x => x.SafetyBufferDays).HasDefaultValue(3);
        b.Property(x => x.IsSeasonal).HasDefaultValue(false);

        // SKU must not repeat — the database enforces it better than code.
        b.HasIndex(x => x.Sku).IsUnique();

        // A product belongs to one category; a category can't be deleted while products reference it.
        b.HasOne(x => x.Category)
         .WithMany(c => c.Products)
         .HasForeignKey(x => x.CategoryId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.CategoryId);
    }
}
