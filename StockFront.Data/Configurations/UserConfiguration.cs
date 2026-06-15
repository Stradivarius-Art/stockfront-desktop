using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFront.Data.Entities;

namespace StockFront.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.Email)
         .IsRequired()
         .HasMaxLength(256);

        b.Property(x => x.PasswordHash)
         .IsRequired()
         .HasMaxLength(256);

        b.Property(x => x.DisplayName)
         .IsRequired()
         .HasMaxLength(200);

        // Three fixed roles — stored as an int (no separate Role table needed).
        b.Property(x => x.Role)
         .HasConversion<int>()
         .IsRequired();

        b.Property(x => x.IsActive)
         .HasDefaultValue(true);

        b.Property(x => x.CreatedAt)
         .HasColumnType("datetime(6)")
         .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        b.Property(x => x.LastLoginAt)
         .HasColumnType("datetime(6)");

        // E-mail is the login, so it must be unique across all accounts.
        b.HasIndex(x => x.Email).IsUnique();
    }
}
