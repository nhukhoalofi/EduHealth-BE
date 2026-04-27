namespace EduHealth.Data.Entities
{
    public class SystemLog
    {
        public long LogId { get; set; }
        public DateTime CreatedAt { get; set; }

        public int? ActorUserId { get; set; }
        public string ActorName { get; set; } = null!;
        public string? ActorUsername { get; set; }
        public string ActorRole { get; set; } = null!;

        public string Module { get; set; } = null!;
        public string Action { get; set; } = null!;

        public string TargetType { get; set; } = null!;
        public string? TargetId { get; set; }
        public string TargetLabel { get; set; } = null!;

        public string Description { get; set; } = null!;
        public string Status { get; set; } = null!;

        public string? MetadataJson { get; set; }

        // Navigation (optional)
        public User? ActorUser { get; set; }
    }
}
