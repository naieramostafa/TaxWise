using MediatR;
using Microsoft.EntityFrameworkCore;
using StreamlineTax.Application.Common.Interfaces;
using StreamlineTax.Application.Common.Models;

namespace StreamlineTax.Application.Tax.Queries.GetTaxSummary;

public class GetTaxSummaryQueryHandler(IApplicationDbContext context) : IRequestHandler<GetTaxSummaryQuery, TaxSummaryDto>
{
    public async Task<TaxSummaryDto> Handle(GetTaxSummaryQuery request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .AsNoTracking()
            .Include(u => u.TaxAccount)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found");

        var transactionCount = await context.Transactions
            .AsNoTracking()
            .CountAsync(t => t.UserId == request.UserId, cancellationToken);

        // Tax reserve is always calculated from underlying transaction data
        var totalReserved = await context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == request.UserId)
            .SumAsync(t => (decimal?)t.TaxWithheld, cancellationToken) ?? 0m;

        var taxAccount = user.TaxAccount;
        if (taxAccount is null)
        {
            return new TaxSummaryDto(0, 0, 0, 0, user.TaxWithholdingRate, transactionCount, 0, true);
        }

        var availableReserve = totalReserved - taxAccount.EstimatedTaxDue;

        return new TaxSummaryDto(
            taxAccount.TotalIncome,
            taxAccount.TotalTaxWithheld,
            taxAccount.EstimatedTaxDue,
            taxAccount.Balance,
            user.TaxWithholdingRate,
            transactionCount,
            Math.Abs(availableReserve),
            availableReserve >= 0
        );
    }
}