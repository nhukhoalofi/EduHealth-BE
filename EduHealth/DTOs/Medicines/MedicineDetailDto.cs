namespace EduHealth.DTOs.Medicines
{
    public class MedicineDetailDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? ActiveIngredient { get; set; }
        public string Unit { get; set; } = null!;
        public string? Packaging { get; set; }
        public int WarningThreshold { get; set; }
        public int CurrentStock { get; set; }
        public DateOnly? NearestExpiryDate { get; set; }
        public string Status { get; set; } = null!;
        public string? Note { get; set; }
        public bool IsLowStock { get; set; }
        public bool IsExpiringSoon { get; set; }
        public IReadOnlyList<MedicineBatchItemDto> Batches { get; set; } = Array.Empty<MedicineBatchItemDto>();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
