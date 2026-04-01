namespace EduHealth.DTOs.Examinations
{
    public class ExaminationListQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? StudentId { get; set; }
        public string? ClassId { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public string? DiseaseTypeId { get; set; }
    }
}
