namespace EduHealth.DTOs.Notifications
{
    /// <summary>
    /// SSE (Server-Sent Events) notification event DTO
    /// </summary>
    public class SseNotificationEventDto
    {
        /// <summary>
        /// Event type: NOTIFICATION_CREATED, NOTIFICATION_READ, ALL_NOTIFICATIONS_READ, UNREAD_COUNT_CHANGED
        /// </summary>
        public string EventType { get; set; } = null!;

        /// <summary>
        /// Event timestamp (UTC)
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Event-specific data
        /// </summary>
        public object? Data { get; set; }
    }

    /// <summary>
    /// Notification created event data
    /// </summary>
    public class NotificationCreatedEventData
    {
        public int NotificationId { get; set; }
        public int RecipientCount { get; set; }
    }

    /// <summary>
    /// Notification read event data
    /// </summary>
    public class NotificationReadEventData
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
    }

    /// <summary>
    /// Unread count changed event data
    /// </summary>
    public class UnreadCountChangedEventData
    {
        public int UserId { get; set; }
        public int UnreadCount { get; set; }
    }
}
