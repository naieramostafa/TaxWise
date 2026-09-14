using MediatR;
using Microsoft.EntityFrameworkCore;
using StreamlineTax.Application.Common.Interfaces;
using StreamlineTax.Domain.Entities;
using StreamlineTax.Domain.Enums;

namespace StreamlineTax.Application.Transactions.Queries.GetTransactions;

public class GetTransactionsQueryHandler(IApplicationDbContext context) : IRequestHandler<GetTransactionsQuery, List<Transaction>>
{
    public async Task<List<Transaction>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Transactions.Where(t => t.UserId == request.UserId);

        if (request.Year.HasValue)
        {
            query = query.Where(t => t.TransactionDate.Year == request.Year.Value);
        }

        if (request.Month.HasValue)
        {
            query = query.Where(t => t.TransactionDate.Month == request.Month.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var category = Enum.Parse<TransactionCategory>(request.Category, ignoreCase: true);
            query = query.Where(t => t.Category == category);
        }

        return await query.OrderByDescending(t => t.TransactionDate).ToListAsync(cancellationToken);
    }
}
