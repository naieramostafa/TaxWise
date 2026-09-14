using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamlineTax.Domain.Entities;

namespace StreamlineTax.Infrastructure.Persistence.Configurations;

public class TaxPeriodConfiguration : IEntityTypeConfiguration<TaxPeriod>
{
    public void Configure(EntityTypeBuilder<TaxPeriod> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.TotalIncome).HasColumnType("decimal(18,2)");
        builder.Property(p => p.TotalTaxWithheld).HasColumnType("decimal(18,2)");
        builder.Property(p => p.EstimatedTaxDue).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Balance).HasColumnType("decimal(18,2)");

        builder.HasOne(p => p.TaxAccount)
            .WithMany(t => t.TaxPeriods)
            .HasForeignKey(p => p.TaxAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.TaxAccountId, p.Status });
    }
}