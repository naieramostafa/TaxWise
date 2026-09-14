using MediatR;
using Microsoft.EntityFrameworkCore;
using StreamlineTax.Application.Common.Interfaces;

namespace StreamlineTax.Application.Transactions.Commands.DeleteTransaction;

public record DeleteTransactionCommand(Guid TransactionId, Guid UserId) : IRequest;

public class DeleteTransactionCommandHandler(
    IApplicationDbContext context,
    ITaxPeriodGuard periodGuard,
    ITaxPeriodService taxPeriodService,
    ITaxCalculationService taxCalculation) : IRequestHandler<DeleteTransactionCommand>
{
    public async Task Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        await periodGuard.EnsureTransactionNotLockedAsync(request.TransactionId, request.UserId, cancellationToken);

        var transaction = await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId && t.UserId == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"Transaction {request.TransactionId} not found");

        var periodId = transaction.TaxPeriodId;

        var taxAccount = await context.TaxAccounts
            .FirstOrDefaultAsync(t => t.UserId == request.UserId, cancellationToken);

        if (taxAccount is not null)
        {
            taxAccount.TotalIncome -= transaction.Amount;
            taxAccount.TotalTaxWithheld -= transaction.TaxWithheld;
            taxAccount.EstimatedTaxDue = taxCalculation.EstimateAnnualTaxDue(taxAccount.TotalIncome);
            taxAccount.Balance = taxAccount.TotalTaxWithheld - taxAccount.EstimatedTaxDue;
            taxAccount.UpdatedAt = DateTime.UtcNow;
        }

        context.Transactions.Remove(transaction);
        await context.SaveChangesAsync(cancellationToken);

        if (periodId.HasValue)
            await taxPeriodService.RecalculateAsync(request.UserId, periodId.Value, cancellationToken);
    }
}