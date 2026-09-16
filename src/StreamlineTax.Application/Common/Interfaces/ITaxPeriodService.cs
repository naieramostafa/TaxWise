using StreamlineTax.Application.Common.Models;
using StreamlineTax.Domain.Entities;

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
