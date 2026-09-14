using MediatR;
using Microsoft.EntityFrameworkCore;
using StreamlineTax.Application.Common.Interfaces;

namespace StreamlineTax.Application.Transactions.Commands.CategorizeTransaction;

public class CategorizeTransactionCommandHandler(
    IApplicationDbContext context,
    ITaxPeriodGuard periodGuard) : IRequestHandler<CategorizeTransactionCommand>
{
    public async Task Handle(CategorizeTransactionCommand request, CancellationToken cancellationToken)
    {
        await periodGuard.EnsureTransactionNotLockedAsync(request.TransactionId, request.UserId, cancellationToken);

        var transaction = await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId && t.UserId == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"Transaction {request.TransactionId} not found");

        transaction.Category = request.Category;
        transaction.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }
}