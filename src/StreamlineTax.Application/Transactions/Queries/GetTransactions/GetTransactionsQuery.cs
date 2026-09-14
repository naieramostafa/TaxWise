using MediatR;
using StreamlineTax.Domain.Entities;
using StreamlineTax.Domain.Enums;

namespace StreamlineTax.Application.Transactions.Queries.GetTransactions;

public record GetTransactionsQuery(Guid UserId, int? Year, int? Month, string? Category) : IRequest<List<Transaction>>;