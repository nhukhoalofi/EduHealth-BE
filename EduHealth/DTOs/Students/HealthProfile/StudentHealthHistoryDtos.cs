namespace EduHealth.DTOs.Students.HealthProfile
{
    public class StudentHealthHistoryItemDto
    {
        public string VisitId { get; set; } = null!; // VISxxx
        public DateTime VisitDate { get; set; }
        public NurseBriefDto Nurse { get; set; } = null!;
        public DiseaseBriefDto? DiseaseType { get; set; }
        public string? Symptoms { get; set; }
        public string? Diagnosis { get; set; }
        public string? Treatment { get; set; }
        public string? Note { get; set; }
        public IReadOnlyList<PrescriptionItemDto> Prescriptions { get; set; } = Array.Empty<PrescriptionItemDto>();
    }

    public class NurseBriefDto
    {
        public string UserId { get; set; } = null!; // USRxxx
        public string FullName { get; set; } = null!;
    }

    public class DiseaseBriefDto
    {
        public string Id { get; set; } = null!; // DISxxx
        public string Name { get; set; } = null!;
    }

    public class PrescriptionItemDto
    {
        public string PrescriptionId { get; set; } = null!; // VPxxx
        public string MedicineId { get; set; } = null!; // MEDxxx
        public string MedicineName { get; set; } = null!;
        public int Quantity { get; set; }
        public string? UsageInstruction { get; set; }
    }

    public class StudentHealthHistoryQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
