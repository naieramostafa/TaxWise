using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamlineTax.Domain.Entities;

namespace StreamlineTax.Infrastructure.Persistence.Configurations;

public class TaxAccountConfiguration : IEntityTypeConfiguration<TaxAccount>
{
    public void Configure(EntityTypeBuilder<TaxAccount> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TotalIncome).HasColumnType("decimal(18,2)");
        builder.Property(t => t.TotalTaxWithheld).HasColumnType("decimal(18,2)");
        builder.Property(t => t.EstimatedTaxDue).HasColumnType("decimal(18,2)");
        builder.Property(t => t.Balance).HasColumnType("decimal(18,2)");

        builder.HasOne(t => t.User)
            .WithOne(u => u.TaxAccount)
            .HasForeignKey<TaxAccount>(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.UserId).IsUnique();
    }
}
