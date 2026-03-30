namespace EduHealth.Data.Entities
{
    public class SystemAlert
    {
        public int AlertId { get; set; }
        public string AlertType { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}