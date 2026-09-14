namespace StreamlineTax.Application.Common.Interfaces;

public interface ITaxPeriodGuard
{
    Task EnsureTransactionNotLockedAsync(Guid transactionId, Guid userId, CancellationToken cancellationToken = default);
}