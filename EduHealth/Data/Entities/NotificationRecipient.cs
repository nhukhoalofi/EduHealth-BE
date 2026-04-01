namespace EduHealth.Data.Entities
{
    public class NotificationRecipient
    {
        public int Id { get; set; }
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? SentAt { get; set; }
        public string? Status { get; set; }

        // Navigation
        public Notification Notification { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
