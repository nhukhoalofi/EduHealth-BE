using System.ComponentModel.DataAnnotations;

namespace EduHealth.DTOs.Reports
{
    public class AdminReportDashboardDto
    {
        public ReportHeaderDto Header { get; set; } = new();
        public List<ReportSummaryCardDto> SummaryCards { get; set; } = new();
        public List<ReportChartDataDto> ChartData { get; set; } = new();
        public List<ReportClassRowDto> ClassRows { get; set; } = new();
        public ReportSidePanelDto SidePanel { get; set; } = new();
    }

    public class ReportHeaderDto
    {
        public string Title { get; set; } = "Báo cáo quản trị y tế học đường";
        public string Description { get; set; } = "Đánh giá tổng quát sức khỏe học sinh toàn trường.";
    }

    public class ReportSummaryCardDto
    {
        public string Id { get; set; } = null!;
        public string Label { get; set; } = null!;
        public string Value { get; set; } = null!;
        public string? Note { get; set; }
        public int? Progress { get; set; }
    }

    public class ReportChartDataDto
    {
        public int ClassId { get; set; }
        public string Label { get; set; } = null!;
        public int Stable { get; set; }
        public int FollowUp { get; set; }
        public int HighRisk { get; set; }
        public int StablePct { get; set; }
        public int FollowUpPct { get; set; }
        public int HighRiskPct { get; set; }
    }

    public class ReportClassRowDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = null!;
        public int ClassSize { get; set; }
        public int Stable { get; set; }
        public int FollowUp { get; set; }
        public int HighRisk { get; set; }
        public int VaccinationCompletionRate { get; set; }
        public string RiskLabel { get; set; } = null!;
        public string RiskTone { get; set; } = null!;
        public string RowTone { get; set; } = null!;
    }

    public class ReportSidePanelDto
    {
        public List<HighPriorityAlertDto> HighPriorityAlerts { get; set; } = new();
        public List<LowSupplyDto> LowSupplies { get; set; } = new();
        public List<LowVaccinationCoverageDto> LowVaccinationCoverage { get; set; } = new();
    }

    public class HighPriorityAlertDto
    {
        public string Id { get; set; } = null!;
        public int ClassId { get; set; }
        public string ClassName { get; set; } = null!;
        public string Severity { get; set; } = null!;
        public string SeverityTone { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Metric { get; set; } = null!;
        public string UpdatedAt { get; set; } = null!;
        public string UpdatedAtShort { get; set; } = null!;
    }

    public class LowSupplyDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Remaining { get; set; } = null!;
        public string Tone { get; set; } = null!;
        public string ThresholdLabel { get; set; } = null!;
    }

    public class LowVaccinationCoverageDto
    {
        public string Id { get; set; } = null!;
        public string Label { get; set; } = null!;
        public int Coverage { get; set; }
        public string Tone { get; set; } = null!;
        public string? Note { get; set; }
    }

    public class AdminClassDetailEnvelopeDto
    {
        public AdminClassDetailDto? Detail { get; set; }
    }

    public class AdminClassDetailDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = null!;
        public int StudentCount { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string UrgencyLabel { get; set; } = null!;
        public string UrgencyTone { get; set; } = null!;
        public RecipientStatsDto RecipientStats { get; set; } = new();
        public ClassDistributionDto Distribution { get; set; } = new();
        public ClassVaccinationDto Vaccination { get; set; } = new();
        public List<string> HighlightedIssues { get; set; } = new();
        public List<RiskAnalysisItemDto> RiskAnalysis { get; set; } = new();
    }

    public class RecipientStatsDto
    {
        public int Students { get; set; }
    }

    public class ClassDistributionDto
    {
        public int Stable { get; set; }
        public int FollowUp { get; set; }
        public int HighRisk { get; set; }
        public int StablePct { get; set; }
        public int FollowUpPct { get; set; }
        public int HighRiskPct { get; set; }
    }

    public class ClassVaccinationDto
    {
        public int CompletionRate { get; set; }
        public int Completed { get; set; }
        public int Pending { get; set; }
        public string StatusLabel { get; set; } = null!;
        public string StatusTone { get; set; } = null!;
    }

    public class RiskAnalysisItemDto
    {
        public string Id { get; set; } = null!;
        public string Tone { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
    }

    public class NurseReportDashboardDto
    {
        public ReportHeaderDto Header { get; set; } = new()
        {
            Title = "Báo cáo y tế tổng hợp",
            Description = "Phân tích tình hình sức khỏe học sinh và hoạt động y tế theo lớp học."
        };

        public NurseAppliedFiltersDto AppliedFilters { get; set; } = new();
        public NurseFilterOptionsDto FilterOptions { get; set; } = new();
        public List<NurseTrendDto> Trend { get; set; } = new();
        public List<NurseDiseaseBreakdownDto> DiseaseBreakdown { get; set; } = new();
        public List<NurseTopMedicineDto> TopMedicines { get; set; } = new();
        public List<NurseRiskAlertDto> RiskAlerts { get; set; } = new();
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
        public List<NurseClassOptionDto> ClassOptions { get; set; } = new();
    }

    public class NurseClassOptionDto
    {
        public string Value { get; set; } = null!;
        public string Label { get; set; } = null!;
    }

    public class NurseTrendDto
    {
        public string Label { get; set; } = null!;
        public int Value { get; set; }
    }

    public class NurseDiseaseBreakdownDto
    {
        public string Id { get; set; } = null!;
        public string Label { get; set; } = null!;
        public int Count { get; set; }
    }

    public class NurseTopMedicineDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Category { get; set; } = string.Empty;
        public int UsedQuantity { get; set; }
        public int DeltaPercent { get; set; } = 0;
        public string Trend { get; set; } = "stable";
        public string StockStatus { get; set; } = "normal"; // low | normal
    }

    public class NurseRiskAlertDto
    {
        public string Id { get; set; } = null!;
        public string Tone { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string TimeLabel { get; set; } = null!;
    }

    public class NurseClassRowDto
    {
        public string Id { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public string Grade { get; set; } = "all";
        public string GradeLabel { get; set; } = null!;
        public int StudentCount { get; set; }
        public int ExaminationCount { get; set; }
        public int TrackingCount { get; set; }
        public int MedicineDispenseCount { get; set; }
        public int VaccinationRate { get; set; }
        public string Status { get; set; } = "safe"; // alert | watch | safe
    }

    public class NurseReportFilterDto
    {
        public string TimeRange { get; set; } = "this-month";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Grade { get; set; } = "all";
        public string ClassId { get; set; } = "all";
        public string ReportType { get; set; } = "overview";
    }

    public class NurseReportExportRequestDto : NurseReportFilterDto
    {
        public string Format { get; set; } = "xlsx";
    }

    public class HealthSummaryDto
    {
        public int Healthy { get; set; }
        public int Sick { get; set; }
        public int Chronic { get; set; }
    }

    public class ReportClassDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = null!;
        public string TeacherName { get; set; } = null!;
        public int TotalStudents { get; set; }
        public HealthSummaryDto HealthBreakdown { get; set; } = new();
        public List<RiskStudentDto> RiskList { get; set; } = new();
    }

    public class RiskStudentDto
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = null!;
        public string RiskLevel { get; set; } = null!;
        public string Description { get; set; } = null!;
    }

    public class ExportRequestDto
    {
        [Required]
        [RegularExpression("^(xlsx|pdf)$", ErrorMessage = "Format must be xlsx or pdf")]
        public string Format { get; set; } = "xlsx";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? ClassId { get; set; }
    }

    public class ExportFileDto
    {
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        public byte[] FileBytes { get; set; } = Array.Empty<byte>();
    }

    public class DirectiveRequestDto
    {
        [Required] public string Title { get; set; } = null!;
        [Required] public string Content { get; set; } = null!;
        public int? ClassId { get; set; }
        public List<int>? RecipientUserIds { get; set; }
    }

    public class DirectiveResponseDto
    {
        public int DirectiveId { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class SystemLogSummaryDto
    {
        public int TotalLogs { get; set; }
        public List<LogBreakdownDto> Breakdowns { get; set; } = new();
    }

    public class LogBreakdownDto
    {
        public string Module { get; set; } = null!;
        public string Action { get; set; } = null!;
        public string Role { get; set; } = null!;
        public int Count { get; set; }
    }

    public class AdminNotificationPreviewRequestDto
    {
        public int? ClassId { get; set; }
        public List<int>? RecipientUserIds { get; set; }
    }

    public class AdminNotificationPreviewResponseDto
    {
        public int TotalRecipients { get; set; }
        public List<NotificationRecipientPreviewDto> Recipients { get; set; } = new();
    }

    public class NotificationRecipientPreviewDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Role { get; set; } = null!;
    }

    public class AdminNotificationRequestDto
    {
        [Required] public string Title { get; set; } = null!;
        [Required] public string Content { get; set; } = null!;
        [Required] public string Type { get; set; } = null!;
        public int? ClassId { get; set; }
        public List<int>? RecipientUserIds { get; set; }
        public int? RelatedDirectiveId { get; set; }
    }

    public class AdminNotificationResponseDto
    {
        public int NotificationId { get; set; }
        public string Status { get; set; } = null!;
        public int RecipientCount { get; set; }
    }
}