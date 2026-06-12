using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFront.Data.Entities;

namespace StockFront.Data.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.Name)
         .IsRequired()
         .HasMaxLength(100);
    }
}
