using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamlineTax.Domain.Entities;

namespace StreamlineTax.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Description).HasMaxLength(500).IsRequired();
        builder.Property(t => t.Amount).HasColumnType("decimal(18,2)");
        builder.Property(t => t.TaxWithheld).HasColumnType("decimal(18,2)");
        builder.Property(t => t.Category).HasConversion<string>().HasMaxLength(50);

        builder.HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Receipt)
            .WithMany()
            .HasForeignKey(t => t.ReceiptId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.TaxPeriod)
            .WithMany(p => p.Transactions)
            .HasForeignKey(t => t.TaxPeriodId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.TaxPeriodId);
    }
}
