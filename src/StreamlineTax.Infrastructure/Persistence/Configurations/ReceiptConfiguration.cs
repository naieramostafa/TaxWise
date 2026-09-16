using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamlineTax.Domain.Entities;

namespace StreamlineTax.Infrastructure.Persistence.Configurations;

public class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.FileName).HasMaxLength(255).IsRequired();
        builder.Property(r => r.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(r => r.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(r => r.MerchantName).HasMaxLength(255);
        builder.Property(r => r.FileHash).HasMaxLength(64);
        builder.HasIndex(r => new { r.UserId, r.FileHash });
        builder.HasIndex(r => new { r.UserId, r.CreatedAt });

        builder.HasOne(r => r.User)
            .WithMany(u => u.Receipts)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
