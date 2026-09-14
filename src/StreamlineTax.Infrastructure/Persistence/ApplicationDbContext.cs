using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StreamlineTax.Application.Common.Interfaces;
using StreamlineTax.Domain.Entities;

namespace StreamlineTax.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options), IApplicationDbContext
{
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TaxAccount> TaxAccounts => Set<TaxAccount>();
    public DbSet<TaxPeriod> TaxPeriods => Set<TaxPeriod>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    DbSet<AppUser> IApplicationDbContext.Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.Property(u => u.Name).HasMaxLength(100);
            entity.Property(u => u.TaxWithholdingRate).HasDefaultValue(0.25m);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.Token).HasMaxLength(256);
            entity.HasIndex(rt => rt.Token).IsUnique();
            entity.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
