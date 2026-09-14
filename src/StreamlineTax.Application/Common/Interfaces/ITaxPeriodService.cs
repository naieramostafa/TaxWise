using StreamlineTax.Domain.Entities;
using StreamlineTax.Domain.Enums;

namespace StreamlineTax.Application.Common.Interfaces;

public interface ITaxPeriodService
{
    Task<TaxPeriod?> GetPeriodForDateAsync(Guid userId, DateTime date, CancellationToken cancellationToken = default);
    Task<TaxPeriodDto?> GetByIdAsync(Guid userId, Guid periodId, CancellationToken cancellationToken = default);
    Task<List<TaxPeriodDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<TaxPeriodComparisonDto> GetComparisonAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<TaxPeriodDto> CloseAsync(Guid userId, Guid periodId, CancellationToken cancellationToken = default);
    Task<TaxPeriodDto> LockAsync(Guid userId, Guid periodId, CancellationToken cancellationToken = default);
    Task RecalculateAsync(Guid userId, Guid periodId, CancellationToken cancellationToken = default);
}

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