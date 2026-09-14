using Microsoft.EntityFrameworkCore;
using StreamlineTax.Application.Common.Exceptions;
using StreamlineTax.Application.Common.Interfaces;
using StreamlineTax.Domain.Enums;

namespace StreamlineTax.Infrastructure.Services;

public class TaxPeriodGuard(IApplicationDbContext context) : ITaxPeriodGuard
{
    public async Task EnsureTransactionNotLockedAsync(Guid transactionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var periodStatus = await context.Transactions
            .Where(t => t.Id == transactionId && t.UserId == userId)
            .Select(t => t.TaxPeriod != null ? t.TaxPeriod.Status : (TaxPeriodStatus?)null)
            .FirstOrDefaultAsync(cancellationToken);

        if (periodStatus == TaxPeriodStatus.Locked)
            throw new TaxPeriodLockedException(
                "This transaction belongs to a locked tax period and cannot be modified.");
    }
}