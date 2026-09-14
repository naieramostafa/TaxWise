using MediatR;

namespace StreamlineTax.Application.Demo.SeedDemoData;

public record SeedDemoDataCommand(Guid UserId) : IRequest<SeedDemoDataResult>;

public record SeedDemoDataResult(int TransactionsCreated, decimal TotalIncome, decimal TotalTaxWithheld);
