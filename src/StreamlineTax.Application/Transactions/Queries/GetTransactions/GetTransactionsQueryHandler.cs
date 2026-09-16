using MediatR;
using Microsoft.EntityFrameworkCore;
using StreamlineTax.Application.Common.Interfaces;
using StreamlineTax.Domain.Entities;
using StreamlineTax.Domain.Enums;

namespace StreamlineTax.Application.Transactions.Queries.GetTransactions;

public class GetTransactionsQueryHandler(IApplicationDbContext context) : IRequestHandler<GetTransactionsQuery, PaginatedResult<Transaction>>
{
    public async Task<PaginatedResult<Transaction>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == request.UserId);

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

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
        var page = Math.Max(1, Math.Min(request.Page, totalPages));

        var items = await query
            .OrderByDescending(t => t.TransactionDate)
            .Skip((page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<Transaction>(
            items,
            totalCount,
            page,
            request.PageSize,
            totalPages);
    }
}
