namespace EduHealth.DTOs.Dashboard
{
    public class DashboardSummaryDto
    {
        public int TotalStudents { get; set; }
        public int TotalClasses { get; set; }
        public int TotalMedicines { get; set; }
        public int LowStockMedicines { get; set; }
        public int TotalVisitsToday { get; set; }
        public int TotalVisitsThisMonth { get; set; }
    }

    public class TopDiseaseDto
    {
        public string DiseaseName { get; set; } = null!;
        public int Count { get; set; }
    }

    public class RecentVisitDto
    {
        public string VisitCode { get; set; } = null!;
        public string StudentCode { get; set; } = null!;
        public string StudentName { get; set; } = null!;
        public string Diagnosis { get; set; } = null!;
        public DateTime VisitDate { get; set; }
    }
}