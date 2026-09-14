using Microsoft.EntityFrameworkCore;
using StreamlineTax.Domain.Entities;

namespace StreamlineTax.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<AppUser> Users { get; }
    DbSet<Receipt> Receipts { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<TaxAccount> TaxAccounts { get; }
    DbSet<TaxPeriod> TaxPeriods { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
