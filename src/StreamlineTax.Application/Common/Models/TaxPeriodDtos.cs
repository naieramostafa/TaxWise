using StreamlineTax.Domain.Enums;

namespace StreamlineTax.Application.Common.Models;

public record TaxPeriodDto(
    Guid Id,
    Guid TaxAccountId,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    TaxPeriodStatus Status,
    decimal TotalIncome,
    decimal TotalTaxWithheld,
    decimal EstimatedTaxDue,
    decimal Balance,
    int TransactionCount,
    DateTime? ClosedAt,
    DateTime? LockedAt);

public record TaxPeriodDetailDto(
    TaxPeriodDto Period,
    List<TransactionDto> Transactions);

public record TransactionDto(
    Guid Id,
    decimal Amount,
    string Description,
    string Category,
    DateTime TransactionDate,
    decimal TaxWithheld);

public record TaxPeriodComparisonDto(
    TaxPeriodDto? Current,
    TaxPeriodDto? Previous,
    decimal? IncomeChangePercent,
    decimal? TaxWithheldChangePercent,
    decimal? EstimatedTaxDueChangePercent,
    int? TransactionCountChangePercent);
