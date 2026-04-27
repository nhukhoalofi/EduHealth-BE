namespace EduHealth.Services.Interfaces
{
    /// <summary>
    /// Service để quản lý Server-Sent Events (SSE) connections cho notifications
    /// </summary>
    public interface ISseNotificationService
    {
        /// <summary>
        /// Thêm client vào danh sách theo dõi
        /// </summary>
        Task AddClientAsync(int userId, IAsyncEnumerable<string> channel, CancellationToken cancellationToken);

        /// <summary>
        /// Xóa client khỏi danh sách theo dõi
        /// </summary>
        Task RemoveClientAsync(int userId);

        /// <summary>
        /// Phát sự kiện thông báo mới đến client
        /// </summary>
        Task BroadcastNotificationCreatedAsync(int notificationId, int[] recipientUserIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Phát sự kiện đánh dấu đã đọc đến client
        /// </summary>
        Task BroadcastNotificationReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Phát sự kiện đánh dấu tất cả đã đọc đến client
        /// </summary>
        Task BroadcastAllNotificationsReadAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Phát sự kiện unread count change
        /// </summary>
        Task BroadcastUnreadCountChangeAsync(int userId, int unreadCount, CancellationToken cancellationToken = default);
    }
}
