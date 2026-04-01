namespace EduHealth.DTOs.Notifications
{
    public class CreateNotificationRequestDto
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string Type { get; set; } = null!;

        public int? ClassId { get; set; }
        public int? DiseaseId { get; set; }
        public int? VaccinationId { get; set; }

        public IReadOnlyList<int>? RecipientUserIds { get; set; }
    }
}
