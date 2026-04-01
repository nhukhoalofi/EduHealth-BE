namespace EduHealth.DTOs.Medicines
{
    public class MedicineListQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Keyword { get; set; }
        public string? Status { get; set; }
        public bool? LowStock { get; set; }
        public bool? Expiring { get; set; }
    }
}
