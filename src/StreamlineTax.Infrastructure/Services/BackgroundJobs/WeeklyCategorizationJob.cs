using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreamlineTax.Application.Common.Interfaces;
using StreamlineTax.Domain.Enums;

namespace StreamlineTax.Infrastructure.Services.BackgroundJobs;

public class WeeklyCategorizationJob(
    IApplicationDbContext context,
    INotificationService notificationService,
    ILogger<WeeklyCategorizationJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var sinceUtc = now.AddDays(-7);

        var userIds = await context.Users.Select(u => u.Id).ToListAsync(cancellationToken);

        foreach (var userId in userIds)
        {
            var uncategorizedCount = await context.Transactions
                .CountAsync(t => t.UserId == userId && t.Category == TransactionCategory.Uncategorized, cancellationToken);

            if (uncategorizedCount == 0)
                continue;

            // Avoid notification spam: only one weekly reminder per user per week
            if (await notificationService.ExistsAsync(userId, NotificationType.WeeklyCategorization, sinceUtc, cancellationToken))
                continue;

            await notificationService.CreateAsync(
                userId,
                NotificationType.WeeklyCategorization,
                "Weekly tax review",
                $"You have {uncategorizedCount} uncategorized transaction(s). Review them now to keep your tax records up to date.",
                relatedEntityType: "Transactions",
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Weekly categorization reminder created for user {UserId} ({Count} uncategorized)",
                userId, uncategorizedCount);
        }
    }
}