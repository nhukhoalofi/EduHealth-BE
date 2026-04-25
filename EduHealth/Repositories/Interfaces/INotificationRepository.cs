using EduHealth.Data.Entities;

namespace EduHealth.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        Task<List<Student>> GetRecipientsByClassIdAsync(int classId, CancellationToken cancellationToken = default);
        Task<List<Student>> GetRecipientsByUserIdsAsync(IReadOnlyList<int> userIds, CancellationToken cancellationToken = default);
        Task<List<User>> GetUsersByIdsAsync(IReadOnlyList<int> userIds, CancellationToken cancellationToken = default);

        Task AddNotificationAsync(Notification notification, CancellationToken cancellationToken = default);
        Task AddRecipientsAsync(IReadOnlyList<NotificationRecipient> recipients, CancellationToken cancellationToken = default);
        Task<bool> RecipientExistsAsync(int userId, int notificationId, CancellationToken cancellationToken = default);

        Task<NotificationRecipient?> GetRecipientAsync(int userId, int notificationId, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
