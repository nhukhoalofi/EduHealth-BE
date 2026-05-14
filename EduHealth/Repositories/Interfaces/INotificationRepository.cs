using EduHealth.Data.Entities;

namespace EduHealth.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        Task<List<Student>> GetRecipientsByClassIdAsync(int classId, CancellationToken cancellationToken = default);
        Task<List<Student>> GetRecipientsByUserIdsAsync(IReadOnlyList<int> userIds, CancellationToken cancellationToken = default);
        Task<List<User>> GetUsersByIdsAsync(IReadOnlyList<int> userIds, CancellationToken cancellationToken = default);
        Task<List<User>> GetUsersByRolesAsync(IReadOnlyList<string> roles, CancellationToken cancellationToken = default);
        Task<bool> ClassExistsAsync(int classId, CancellationToken cancellationToken = default);

        Task AddNotificationAsync(Notification notification, CancellationToken cancellationToken = default);
        Task AddRecipientsAsync(IReadOnlyList<NotificationRecipient> recipients, CancellationToken cancellationToken = default);
        Task<bool> RecipientExistsAsync(int userId, int notificationId, CancellationToken cancellationToken = default);

        Task<NotificationRecipient?> GetRecipientAsync(int userId, int notificationId, CancellationToken cancellationToken = default);

        Task<(List<NotificationRecipient> Items, int Total)> GetNotificationsForUserAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<(List<Notification> Items, int Total)> GetSentNotificationsAsync(int createdByUserId, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<(List<Notification> Items, int Total)> GetPublicNotificationsAsync(string? type, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);
        Task<int> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

