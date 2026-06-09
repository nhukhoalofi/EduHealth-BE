namespace EduHealth.DTOs.Medicines
{
    public class DisposeMedicineRequestDto
    {
        public int Quantity { get; set; }
        public string? BatchId { get; set; }
        public string Reason { get; set; } = null!;
        public DateOnly? ExpiryDate { get; set; }
        public string? BatchNumber { get; set; }
        public string? Note { get; set; }
    }
}
