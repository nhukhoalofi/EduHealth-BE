namespace EduHealth.DTOs.Medicines
{
    public class MedicineBatchItemDto
    {
        public string Id { get; set; } = null!;
        public string? BatchNumber { get; set; }
        public DateTime ReceivedAt { get; set; }
        public DateOnly ExpiryDate { get; set; }
        public int InitialQuantity { get; set; }
        public int RemainingQuantity { get; set; }
        public string Status { get; set; } = null!;
        public string? Note { get; set; }
        public bool IsExpiringSoon { get; set; }
        public bool IsExpired { get; set; }
        public bool IsFefoPriority { get; set; }
    }
}
