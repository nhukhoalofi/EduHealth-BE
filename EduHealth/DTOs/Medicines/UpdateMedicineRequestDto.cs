namespace EduHealth.DTOs.Medicines
{
    public class UpdateMedicineRequestDto
    {
        public string? Name { get; set; }
        public string? ActiveIngredient { get; set; }
        public string? Unit { get; set; }
        public string? Packaging { get; set; }
        public int? WarningThreshold { get; set; }
        public string? Note { get; set; }
    }
}
