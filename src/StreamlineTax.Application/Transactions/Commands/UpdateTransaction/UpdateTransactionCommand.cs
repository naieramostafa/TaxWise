using MediatR;
using Microsoft.EntityFrameworkCore;
using StreamlineTax.Application.Common.Interfaces;
using StreamlineTax.Domain.Enums;

namespace StreamlineTax.Application.Transactions.Commands.UpdateTransaction;

public record UpdateTransactionCommand(
    Guid TransactionId,
    Guid UserId,
    decimal Amount,
    string Description,
    DateTime TransactionDate,
    TransactionCategory Category) : IRequest;

public class UpdateTransactionCommandHandler(
    IApplicationDbContext context,
    ITaxPeriodGuard periodGuard,
    ITaxPeriodService taxPeriodService,
    ITaxCalculationService taxCalculation) : IRequestHandler<UpdateTransactionCommand>
{
    public async Task Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        await periodGuard.EnsureTransactionNotLockedAsync(request.TransactionId, request.UserId, cancellationToken);

        var transaction = await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId && t.UserId == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"Transaction {request.TransactionId} not found");

        var oldPeriodId = transaction.TaxPeriodId;
        var oldWithheld = transaction.TaxWithheld;
        var oldAmount = transaction.Amount;

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found");

        var newPeriod = await taxPeriodService.GetPeriodForDateAsync(request.UserId, request.TransactionDate, cancellationToken)
            ?? throw new KeyNotFoundException($"No tax period found for date {request.TransactionDate}");

        transaction.Amount = request.Amount;
        transaction.Description = request.Description;
        transaction.TransactionDate = request.TransactionDate;
        transaction.Category = request.Category;
        transaction.TaxWithheld = taxCalculation.CalculateTaxWithholding(request.Amount, user.TaxWithholdingRate);
        transaction.TaxPeriodId = newPeriod.Id;
        transaction.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        var taxAccount = await context.TaxAccounts
            .FirstOrDefaultAsync(t => t.UserId == request.UserId, cancellationToken);

        if (taxAccount is not null)
        {
            taxAccount.TotalIncome += request.Amount - oldAmount;
            taxAccount.TotalTaxWithheld += transaction.TaxWithheld - oldWithheld;
            taxAccount.EstimatedTaxDue = taxCalculation.EstimateAnnualTaxDue(taxAccount.TotalIncome);
            taxAccount.Balance = taxAccount.TotalTaxWithheld - taxAccount.EstimatedTaxDue;
            taxAccount.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);

        await taxPeriodService.RecalculateAsync(request.UserId, newPeriod.Id, cancellationToken);
        if (oldPeriodId.HasValue && oldPeriodId.Value != newPeriod.Id)
            await taxPeriodService.RecalculateAsync(request.UserId, oldPeriodId.Value, cancellationToken);
    }
}