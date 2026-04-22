using System.ComponentModel.DataAnnotations;

namespace EduHealth.DTOs.Reports
{
    public class AdminReportDashboardDto
    {
        public int TotalStudents { get; set; }
        public int TotalClasses { get; set; }
        public int TotalHealthVisits { get; set; }
        public int TotalVaccinationCampaigns { get; set; }
        public int MedicineWarningsCount { get; set; }
        public int NotificationsSentCount { get; set; }
        public int SystemLogsCount { get; set; }
        public HealthSummaryDto HealthSummary { get; set; } = new();
    }

    public class NurseReportDashboardDto
    {
        public int TotalAssignedStudents { get; set; }
        public int TotalAssignedClasses { get; set; }
        public int TodayHealthVisits { get; set; }
        public int ExpiringMedicinesCount { get; set; }
        public int PendingVaccinationsCount { get; set; }
        public HealthSummaryDto HealthSummary { get; set; } = new();
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
        [RegularExpression("^(pdf|xlsx)$", ErrorMessage = "Format must be pdf or xlsx")]
        public string Format { get; set; } = "pdf";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? ClassId { get; set; }
    }

    public class ExportResponseDto
    {
        public string FileName { get; set; } = null!;
        public string DownloadUrl { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }

    public class DirectiveRequestDto
    {
        public int? ClassId { get; set; }
        [Required]
        public string Title { get; set; } = null!;
        [Required]
        public string Content { get; set; } = null!;
        [Required]
        public string Priority { get; set; } = "NORMAL";
        public DateTime? RelatedReportDate { get; set; }
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
        [Required]
        public string Title { get; set; } = null!;
        [Required]
        public string Content { get; set; } = null!;
        [Required]
        public string Type { get; set; } = null!;
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