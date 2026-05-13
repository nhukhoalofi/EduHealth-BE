namespace EduHealth.DTOs.Notifications
{
    public class NotificationItemDto
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? Image { get; set; }
        public string Type { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public string CreatedByUserName { get; set; } = null!;

        public int? ClassId { get; set; }
        public int? DiseaseId { get; set; }
        public int? VaccinationId { get; set; }

        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
