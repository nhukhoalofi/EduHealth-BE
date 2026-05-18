namespace EduHealth.Data.Entities
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? Image { get; set; }
        public string Type { get; set; } = null!;
        public string Visibility { get; set; } = "INTERNAL";
        public string Status { get; set; } = "PUBLISHED";
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }

        public int? ClassId { get; set; }
        public int? DiseaseId { get; set; }
        public int? VaccinationId { get; set; }

        // Navigation
        public User CreatedByUser { get; set; } = null!;
        public SchoolClass? Class { get; set; }
        public DiseaseType? DiseaseType { get; set; }
        public Vaccination? Vaccination { get; set; }
        public ICollection<NotificationRecipient> Recipients { get; set; } = new List<NotificationRecipient>();
    }
}
