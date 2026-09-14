using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StreamlineTax.Application.Common.Interfaces;
using StreamlineTax.Domain.Entities;
using StreamlineTax.Domain.Enums;
using StreamlineTax.Infrastructure.Hubs;

namespace StreamlineTax.Infrastructure.Services;

public class NotificationService(
    IApplicationDbContext context,
    IHubContext<TaxNotificationHub> hubContext) : INotificationService
{
    public async Task<NotificationDto> CreateAsync(Guid userId, NotificationType type, string title, string message,
        Guid? relatedEntityId = null, string? relatedEntityType = null, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync(cancellationToken);

        var dto = ToDto(notification);
        await hubContext.Clients.Group(userId.ToString())
            .SendAsync("NotificationCreated", dto, cancellationToken);

        return dto;
    }

    public async Task<List<NotificationDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto(
                n.Id, n.UserId, n.Type, n.Title, n.Message, n.IsRead, n.CreatedAt, n.ReadAt,
                n.RelatedEntityId, n.RelatedEntityType))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);

        if (notification is null) return;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var unread = await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid userId, NotificationType type, DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return await context.Notifications
            .AnyAsync(n => n.UserId == userId && n.Type == type && n.CreatedAt >= sinceUtc, cancellationToken);
    }

    private static NotificationDto ToDto(Notification n) => new(
        n.Id, n.UserId, n.Type, n.Title, n.Message, n.IsRead, n.CreatedAt, n.ReadAt,
        n.RelatedEntityId, n.RelatedEntityType);
}