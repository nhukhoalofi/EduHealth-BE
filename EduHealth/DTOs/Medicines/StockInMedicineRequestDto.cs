namespace EduHealth.DTOs.Medicines
{
    public class StockInMedicineRequestDto
    {
        public int Quantity { get; set; }
        public DateOnly ExpiryDate { get; set; }
        public string? BatchNumber { get; set; }
        public string? Note { get; set; }
    }
}
