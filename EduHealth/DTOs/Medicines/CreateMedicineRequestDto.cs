namespace EduHealth.DTOs.Medicines
{
    public class CreateMedicineRequestDto
    {
        public string Name { get; set; } = null!;
        public string? ActiveIngredient { get; set; }
        public string Unit { get; set; } = null!;
        public string? Packaging { get; set; }
        public int WarningThreshold { get; set; }
        public string? Note { get; set; }
    }
}
