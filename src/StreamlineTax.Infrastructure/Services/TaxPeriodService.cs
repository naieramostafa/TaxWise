using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreamlineTax.Application.Common.Interfaces;
using StreamlineTax.Domain.Entities;
using StreamlineTax.Domain.Enums;

namespace StreamlineTax.Infrastructure.Services;

public class TaxPeriodService(
    IApplicationDbContext context,
    ITaxCalculationService taxCalculation,
    INotificationService notificationService,
    ILogger<TaxPeriodService> logger) : ITaxPeriodService
{
    public async Task<TaxPeriod?> GetPeriodForDateAsync(Guid userId, DateTime date, CancellationToken cancellationToken = default)
    {
        var taxAccount = await GetOrCreateTaxAccountAsync(userId, cancellationToken);

        var (start, end, name) = QuarterFor(date);
        var period = await context.TaxPeriods
            .FirstOrDefaultAsync(p => p.TaxAccountId == taxAccount.Id && p.StartDate == start, cancellationToken);

        if (period is null)
        {
            period = new TaxPeriod
            {
                TaxAccountId = taxAccount.Id,
                Name = name,
                StartDate = start,
                EndDate = end
            };
            context.TaxPeriods.Add(period);
            await context.SaveChangesAsync(cancellationToken);
        }

        return period;
    }

    public async Task<TaxAccount> GetOrCreateTaxAccountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var taxAccount = await context.TaxAccounts
            .FirstOrDefaultAsync(t => t.UserId == userId, cancellationToken);

        if (taxAccount is null)
        {
            taxAccount = new TaxAccount { UserId = userId };
            context.TaxAccounts.Add(taxAccount);
            await context.SaveChangesAsync(cancellationToken);
        }

        return taxAccount;
    }

    public async Task<TaxPeriodDto?> GetByIdAsync(Guid userId, Guid periodId, CancellationToken cancellationToken = default)
    {
        var period = await context.TaxPeriods
            .Where(p => p.TaxAccountId == context.TaxAccounts
                .Where(t => t.UserId == userId).Select(t => t.Id).FirstOrDefault())
            .FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken);

        if (period is null) return null;

        if (period.Status == TaxPeriodStatus.Open && period.EndDate < DateTime.UtcNow)
        {
            period.Status = TaxPeriodStatus.Closed;
            period.ClosedAt ??= DateTime.UtcNow;
            period.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Tax period {Name} auto-closed after end date", period.Name);
        }

        var count = await context.Transactions
            .CountAsync(t => t.TaxPeriodId == periodId, cancellationToken);

        return new TaxPeriodDto(
            period.Id,
            period.TaxAccountId,
            period.Name,
            period.StartDate,
            period.EndDate,
            period.Status,
            period.TotalIncome,
            period.TotalTaxWithheld,
            period.EstimatedTaxDue,
            period.Balance,
            count,
            period.ClosedAt,
            period.LockedAt);
    }

    public async Task<List<TaxPeriodDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var taxAccountId = await context.TaxAccounts
            .Where(t => t.UserId == userId).Select(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var periods = await context.TaxPeriods
            .Where(p => p.TaxAccountId == taxAccountId)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(cancellationToken);

        var autoClosed = false;
        foreach (var period in periods)
        {
            if (period.Status == TaxPeriodStatus.Open && period.EndDate < DateTime.UtcNow)
            {
                period.Status = TaxPeriodStatus.Closed;
                period.ClosedAt ??= DateTime.UtcNow;
                period.UpdatedAt = DateTime.UtcNow;
                autoClosed = true;
                logger.LogInformation("Tax period {Name} auto-closed after end date", period.Name);
            }
        }

        if (autoClosed)
            await context.SaveChangesAsync(cancellationToken);

        var periodIds = periods.Select(p => p.Id).ToList();
        var transactionCounts = await context.Transactions
            .Where(t => t.TaxPeriodId.HasValue && periodIds.Contains(t.TaxPeriodId.Value))
            .GroupBy(t => t.TaxPeriodId!.Value)
            .Select(g => new { PeriodId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PeriodId, x => x.Count, cancellationToken);

        var result = periods.Select(period => new TaxPeriodDto(
            period.Id,
            period.TaxAccountId,
            period.Name,
            period.StartDate,
            period.EndDate,
            period.Status,
            period.TotalIncome,
            period.TotalTaxWithheld,
            period.EstimatedTaxDue,
            period.Balance,
            transactionCounts.GetValueOrDefault(period.Id, 0),
            period.ClosedAt,
            period.LockedAt)).ToList();

        return result;
    }

    public async Task<TaxPeriodComparisonDto> GetComparisonAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var periods = await GetForUserAsync(userId, cancellationToken);
        var current = periods.FirstOrDefault();
        var previous = periods.Skip(1).FirstOrDefault();

        return new TaxPeriodComparisonDto(
            current,
            previous,
            CalculatePercent(current?.TotalIncome, previous?.TotalIncome),
            CalculatePercent(current?.TotalTaxWithheld, previous?.TotalTaxWithheld),
            CalculatePercent(current?.EstimatedTaxDue, previous?.EstimatedTaxDue),
            CalculatePercent(current?.TransactionCount, previous?.TransactionCount)
        );
    }

    public async Task<TaxPeriodDto> CloseAsync(Guid userId, Guid periodId, CancellationToken cancellationToken = default)
    {
        var period = await GetOwnedAsync(userId, periodId, cancellationToken);

        if (period.Status == TaxPeriodStatus.Open)
        {
            period.Status = TaxPeriodStatus.Closed;
            period.ClosedAt = DateTime.UtcNow;
            period.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }

        return await ToDtoAsync(period, cancellationToken);
    }

    public async Task<TaxPeriodDto> LockAsync(Guid userId, Guid periodId, CancellationToken cancellationToken = default)
    {
        var period = await GetOwnedAsync(userId, periodId, cancellationToken);

        if (period.Status == TaxPeriodStatus.Open)
        {
            period.Status = TaxPeriodStatus.Closed;
            period.ClosedAt = DateTime.UtcNow;
        }

        if (period.Status == TaxPeriodStatus.Closed)
        {
            period.Status = TaxPeriodStatus.Locked;
            period.LockedAt = DateTime.UtcNow;
            period.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            await notificationService.CreateAsync(
                userId,
                NotificationType.TaxPeriodLocked,
                "Tax period locked",
                $"Your tax period {period.Name} has been locked and is now read-only.",
                period.Id,
                "TaxPeriod",
                cancellationToken);
        }

        return await ToDtoAsync(period, cancellationToken);
    }

    public async Task RecalculateAsync(Guid userId, Guid periodId, CancellationToken cancellationToken = default)
    {
        var taxAccountId = await context.TaxAccounts
            .Where(t => t.UserId == userId).Select(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var period = await context.TaxPeriods
            .Where(p => p.TaxAccountId == taxAccountId)
            .FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken)
            ?? throw new KeyNotFoundException($"Tax period {periodId} not found");

        var transactions = await context.Transactions
            .Where(t => t.TaxPeriodId == periodId)
            .ToListAsync(cancellationToken);

        period.TotalIncome = transactions.Sum(t => t.Amount);
        period.TotalTaxWithheld = transactions.Sum(t => t.TaxWithheld);
        period.EstimatedTaxDue = taxCalculation.EstimateAnnualTaxDue(period.TotalIncome);
        period.Balance = period.TotalTaxWithheld - period.EstimatedTaxDue;
        period.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    // ----- helpers -----

    private async Task<bool> AutoCloseIfEndedAsync(TaxPeriod period, CancellationToken cancellationToken)
    {
        if (period.Status == TaxPeriodStatus.Open && period.EndDate < DateTime.UtcNow)
        {
            period.Status = TaxPeriodStatus.Closed;
            period.ClosedAt ??= DateTime.UtcNow;
            period.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Tax period {Name} auto-closed after end date", period.Name);
            return true;
        }

        return false;
    }

    private async Task<TaxPeriod> GetOwnedAsync(Guid userId, Guid periodId, CancellationToken cancellationToken)
    {
        var taxAccountId = await context.TaxAccounts
            .Where(t => t.UserId == userId).Select(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return await context.TaxPeriods
            .FirstOrDefaultAsync(p => p.Id == periodId && p.TaxAccountId == taxAccountId, cancellationToken)
            ?? throw new KeyNotFoundException($"Tax period {periodId} not found");
    }

    private async Task<TaxPeriodDto> ToDtoAsync(TaxPeriod period, CancellationToken cancellationToken)
    {
        var count = await context.Transactions
            .CountAsync(t => t.TaxPeriodId == period.Id, cancellationToken);

        return new TaxPeriodDto(
            period.Id,
            period.TaxAccountId,
            period.Name,
            period.StartDate,
            period.EndDate,
            period.Status,
            period.TotalIncome,
            period.TotalTaxWithheld,
            period.EstimatedTaxDue,
            period.Balance,
            count,
            period.ClosedAt,
            period.LockedAt);
    }

    private static decimal? CalculatePercent(decimal? current, decimal? previous)
    {
        if (!current.HasValue || !previous.HasValue || previous.Value == 0)
            return null;
        return Math.Round((current.Value - previous.Value) / previous.Value * 100, 1);
    }

    private static int? CalculatePercent(int? current, int? previous)
    {
        if (!current.HasValue || !previous.HasValue || previous.Value == 0)
            return null;
        return (int)Math.Round((current.Value - previous.Value) / (double)previous.Value * 100);
    }

    private static (DateTime Start, DateTime End, string Name) QuarterFor(DateTime date)
    {
        var quarter = (date.Month - 1) / 3 + 1;
        var startMonth = (quarter - 1) * 3 + 1;
        var start = new DateTime(date.Year, startMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(date.Year, startMonth + 3, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(-1);
        var name = $"{date.Year} Q{quarter}";
        return (start, end, name);
    }
}