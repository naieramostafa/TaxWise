using MediatR;
using StreamlineTax.Domain.Entities;
using StreamlineTax.Domain.Enums;

namespace StreamlineTax.Application.Transactions.Queries.GetTransactions;

public record GetTransactionsQuery(
    Guid UserId,
    int? Year,
    int? Month,
    string? Category,
    int Page = 1,
    int PageSize = 50) : IRequest<PaginatedResult<Transaction>>;

public record PaginatedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
