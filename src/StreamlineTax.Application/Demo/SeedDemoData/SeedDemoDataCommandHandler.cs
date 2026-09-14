using MediatR;
using Microsoft.EntityFrameworkCore;
using StreamlineTax.Application.Common.Interfaces;
using StreamlineTax.Domain.Entities;
using StreamlineTax.Domain.Enums;

namespace StreamlineTax.Application.Demo.SeedDemoData;

public class SeedDemoDataCommandHandler(
    IApplicationDbContext context,
    ITaxCalculationService taxCalculation,
    ITaxPeriodService taxPeriodService) : IRequestHandler<SeedDemoDataCommand, SeedDemoDataResult>
{
    public async Task<SeedDemoDataResult> Handle(SeedDemoDataCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found");

        var now = DateTime.UtcNow;
        var seed = new[]
        {
            (Amount: 2500m, Category: TransactionCategory.Salary, Desc: "Salary payment - July", MonthsAgo: 0),
            (Amount: 1200m, Category: TransactionCategory.Freelance, Desc: "Freelance - website redesign", MonthsAgo: 0),
            (Amount: 850m, Category: TransactionCategory.BusinessIncome, Desc: "Business - client consulting", MonthsAgo: 1),
            (Amount: 300m, Category: TransactionCategory.Investment, Desc: "Dividend payout", MonthsAgo: 1),
            (Amount: 1750m, Category: TransactionCategory.Freelance, Desc: "Freelance - mobile app project", MonthsAgo: 2),
            (Amount: 600m, Category: TransactionCategory.OtherIncome, Desc: "Miscellaneous income", MonthsAgo: 2)
        };

        var existing = await context.Transactions
            .CountAsync(t => t.UserId == request.UserId, cancellationToken);

        if (existing == 0)
        {
            foreach (var (amount, category, desc, monthsAgo) in seed)
            {
                var transactionDate = now.AddMonths(-monthsAgo);
                var withheld = taxCalculation.CalculateTaxWithholding(amount, user.TaxWithholdingRate);

                var taxPeriod = await taxPeriodService.GetPeriodForDateAsync(
                    request.UserId, transactionDate, cancellationToken)
                    ?? throw new KeyNotFoundException($"No tax period found for date {transactionDate}");

                context.Transactions.Add(new Transaction
                {
                    UserId = request.UserId,
                    Amount = amount,
                    Description = desc,
                    Category = category,
                    TransactionDate = transactionDate,
                    TaxWithheld = withheld,
                    TaxPeriodId = taxPeriod.Id
                });
            }
        }

        var taxAccount = await context.TaxAccounts
            .FirstOrDefaultAsync(t => t.UserId == request.UserId, cancellationToken);

        if (taxAccount is null)
        {
            taxAccount = new TaxAccount { UserId = request.UserId };
            context.TaxAccounts.Add(taxAccount);
        }

        var totalIncome = seed.Sum(s => s.Amount);
        var totalWithheld = taxCalculation.CalculateTaxWithholding(totalIncome, user.TaxWithholdingRate);

        taxAccount.TotalIncome = totalIncome;
        taxAccount.TotalTaxWithheld = totalWithheld;
        taxAccount.EstimatedTaxDue = taxCalculation.EstimateAnnualTaxDue(totalIncome);
        taxAccount.Balance = totalWithheld - taxAccount.EstimatedTaxDue;
        taxAccount.UpdatedAt = now;

        await context.SaveChangesAsync(cancellationToken);

        // Recalculate each period summary from its underlying transactions
        var periodIds = await context.TaxPeriods
            .Where(p => p.TaxAccountId == taxAccount.Id)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        foreach (var periodId in periodIds)
        {
            await taxPeriodService.RecalculateAsync(request.UserId, periodId, cancellationToken);
        }

        return new SeedDemoDataResult(seed.Length, totalIncome, totalWithheld);
    }
}
