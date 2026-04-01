namespace EduHealth.DTOs.Medicines
{
    public class MedicineListItemDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? ActiveIngredient { get; set; }
        public string Unit { get; set; } = null!;
        public string? Packaging { get; set; }
        public int WarningThreshold { get; set; }
        public int CurrentStock { get; set; }
        public DateOnly? NearestExpiryDate { get; set; }
        public bool IsLowStock { get; set; }
        public bool IsExpiringSoon { get; set; }
        public string Status { get; set; } = null!;
    }
}
