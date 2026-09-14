using StreamlineTax.Domain.Enums;

namespace StreamlineTax.Application.Common.Interfaces;

public interface INotificationService
{
    Task<NotificationDto> CreateAsync(Guid userId, NotificationType type, string title, string message,
        Guid? relatedEntityId = null, string? relatedEntityType = null, CancellationToken cancellationToken = default);
    Task<List<NotificationDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, NotificationType type, DateTime sinceUtc, CancellationToken cancellationToken = default);
}

public record NotificationDto(
    Guid Id,
    Guid UserId,
    NotificationType Type,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt,
    Guid? RelatedEntityId,
    string? RelatedEntityType);