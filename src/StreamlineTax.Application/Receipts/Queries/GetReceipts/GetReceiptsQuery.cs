using MediatR;
using StreamlineTax.Domain.Entities;

namespace StreamlineTax.Application.Receipts.Queries.GetReceipts;

public record GetReceiptsQuery(Guid UserId) : IRequest<List<Receipt>>;
