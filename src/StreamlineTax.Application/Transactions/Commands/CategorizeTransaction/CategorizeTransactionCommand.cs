using MediatR;
using StreamlineTax.Domain.Enums;

namespace StreamlineTax.Application.Transactions.Commands.CategorizeTransaction;

public record CategorizeTransactionCommand(Guid TransactionId, Guid UserId, TransactionCategory Category) : IRequest;
