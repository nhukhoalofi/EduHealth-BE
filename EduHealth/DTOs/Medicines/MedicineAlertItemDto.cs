namespace EduHealth.DTOs.Medicines
{
    public class MedicineAlertItemDto
    {
        public string MedicineId { get; set; } = null!;
        public string MedicineName { get; set; } = null!;
        public string AlertType { get; set; } = null!;
        public int CurrentStock { get; set; }
        public int WarningThreshold { get; set; }
        public DateOnly? NearestExpiryDate { get; set; }
        public string Message { get; set; } = null!;
    }
}
