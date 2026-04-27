namespace EduHealth.DTOs.Reports.Nurse
{
    public class NurseDashboardReportDto
    {
        public ReportHeaderDto Header { get; set; } = new();
        public NurseAppliedFiltersDto AppliedFilters { get; set; } = new();
        public NurseFilterOptionsDto FilterOptions { get; set; } = new();
        public List<TrendItemDto> Trend { get; set; } = new();
        public List<DiseaseBreakdownItemDto> DiseaseBreakdown { get; set; } = new();
        public List<TopMedicineItemDto> TopMedicines { get; set; } = new();
        public List<RiskAlertDto> RiskAlerts { get; set; } = new();
        public List<NurseClassRowDto> ClassRows { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class NurseAppliedFiltersDto
    {
        public string TimeRange { get; set; } = "this-month";
        public string Grade { get; set; } = "all";
        public string ClassId { get; set; } = "all";
        public string ReportType { get; set; } = "overview";
    }

    public class NurseFilterOptionsDto
    {
        public List<FilterOptionItemDto> ClassOptions { get; set; } = new();
        public List<FilterOptionItemDto> GradeOptions { get; set; } = new();
    }

    public class FilterOptionItemDto
    {
        public string Value { get; set; } = null!;
        public string Label { get; set; } = null!;
    }

    public class TrendItemDto
    {
        public string Label { get; set; } = null!;
        public int Value { get; set; }
    }

    public class DiseaseBreakdownItemDto
    {
        public string Id { get; set; } = null!;
        public string Label { get; set; } = null!;
        public int Count { get; set; }
    }

    public class TopMedicineItemDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public int UsedQuantity { get; set; }
        public int DeltaPercent { get; set; }
        public string Trend { get; set; } = "stable";
        public string StockStatus { get; set; } = "normal";
    }

    public class RiskAlertDto
    {
        public string Id { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Severity { get; set; } = "warning";
    }

    public class NurseClassRowDto
    {
        public int Id { get; set; }
        public string ClassName { get; set; } = null!;
        public string Grade { get; set; } = null!;
        public string GradeLabel { get; set; } = null!;
        public int StudentCount { get; set; }
        public int ExaminationCount { get; set; }
        public int TrackingCount { get; set; }
        public int MedicineDispenseCount { get; set; }
        public int VaccinationRate { get; set; }
        public string Status { get; set; } = "safe";
    }

    public class NurseClassDetailReportDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = null!;
        public string Grade { get; set; } = null!;
        public int StudentCount { get; set; }
        public int ExaminationCount { get; set; }
        public List<StudentHealthSummaryDto> Students { get; set; } = new();
        public List<DiseaseBreakdownItemDto> DiseaseBreakdown { get; set; } = new();
        public List<TopMedicineItemDto> MedicinesUsed { get; set; } = new();
        public VaccinationSummaryDto Vaccination { get; set; } = new();
    }

    public class StudentHealthSummaryDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public int? ExaminationCount { get; set; }
        public string? LastDiagnosis { get; set; }
        public DateTime? LastVisitDate { get; set; }
    }

    public class VaccinationSummaryDto
    {
        public int TotalRecords { get; set; }
        public int CompletedCount { get; set; }
        public int PendingCount { get; set; }
        public int CompletionRate { get; set; }
    }
}
