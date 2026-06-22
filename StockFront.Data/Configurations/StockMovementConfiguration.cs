using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFront.Data.Entities;

namespace StockFront.Data.Configurations;

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> b)
    {
        b.HasKey(x => x.Id);

        // Stored as strings ("Receipt"/"WriteOff"/"ReversalReceipt"/…) so journal rows read plainly.
        b.Property(x => x.Type)
         .HasConversion<string>()
         .HasMaxLength(20)
         .IsRequired();

        b.Property(x => x.Reason)
         .HasConversion<string>()
         .HasMaxLength(20);

        b.Property(x => x.Quantity).IsRequired();

        b.Property(x => x.Comment).HasMaxLength(300);

        b.Property(x => x.PerformedBy).HasMaxLength(200);

        b.Property(x => x.CreatedAt)
         .HasColumnType("datetime(6)")
         .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        // A product can't be deleted while movements reference it — the same Restrict rule as order lines.
        b.HasOne(x => x.Product)
         .WithMany()
         .HasForeignKey(x => x.ProductId)
         .OnDelete(DeleteBehavior.Restrict);

        // A reversal points back at the movement it undoes (self-reference); the original must survive.
        b.HasOne<StockMovement>()
         .WithMany()
         .HasForeignKey(x => x.ReversesMovementId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.ProductId);
        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => x.ReversesMovementId);
    }
}
