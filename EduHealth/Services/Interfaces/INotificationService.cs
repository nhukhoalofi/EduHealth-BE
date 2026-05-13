using EduHealth.DTOs.Notifications;

namespace EduHealth.Services.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationRecipientsPreviewResponseDto> PreviewRecipientsAsync(NotificationRecipientsPreviewRequestDto request, CancellationToken cancellationToken = default);
        Task<CreateNotificationResponseDto> CreateAsync(int createdByUserId, CreateNotificationRequestDto request, CancellationToken cancellationToken = default);
        Task<bool> MarkReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default);
        Task<GetNotificationsResponseDto> GetNotificationsAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<int> MarkAllReadAsync(int userId, CancellationToken cancellationToken = default);
    }
}
