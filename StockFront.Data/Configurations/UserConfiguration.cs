using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFront.Data.Entities;

namespace StockFront.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.Username)
         .IsRequired()
         .HasMaxLength(64);

        b.Property(x => x.PasswordHash)
         .IsRequired()
         .HasMaxLength(256);

        b.Property(x => x.DisplayName)
         .IsRequired()
         .HasMaxLength(200);

        // Stored as a string ("Operator", "Customer") for readable rows and filters.
        b.Property(x => x.Role)
         .HasConversion<string>()
         .HasMaxLength(20)
         .IsRequired();

        b.Property(x => x.IsActive)
         .HasDefaultValue(true);

        b.Property(x => x.CreatedAt)
         .HasColumnType("datetime(6)")
         .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        // Login must be unique — the database enforces it better than code.
        b.HasIndex(x => x.Username).IsUnique();
    }
}
