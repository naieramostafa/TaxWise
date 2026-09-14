using MediatR;
using Microsoft.EntityFrameworkCore;
using StreamlineTax.Application.Common.Interfaces;
using StreamlineTax.Domain.Entities;

namespace StreamlineTax.Application.Receipts.Queries.GetReceipts;

public class GetReceiptsQueryHandler(IApplicationDbContext context) : IRequestHandler<GetReceiptsQuery, List<Receipt>>
{
    public async Task<List<Receipt>> Handle(GetReceiptsQuery request, CancellationToken cancellationToken)
    {
        return await context.Receipts
            .Where(r => r.UserId == request.UserId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
