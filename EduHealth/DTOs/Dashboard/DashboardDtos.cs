namespace EduHealth.DTOs.Dashboard
{
    public class TopDiseaseDto
    {
        public string DiseaseName { get; set; } = null!;
        public int Count { get; set; }
    }

    public class AdminDashboardOverviewDto
    {
        public int TotalStudents { get; set; }
        public int TotalClasses { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int LockedUsers { get; set; }
        public int TotalMedicines { get; set; }
        public int LowStockMedicinesCount { get; set; }
        public int TotalVisitsToday { get; set; }
        public int TotalVisitsThisMonth { get; set; }
        public int VaccinationCampaignsActive { get; set; }
    }

    public class AdminDashboardFilterDto
    {
        /// <summary>
        /// Filter by class ID
        /// </summary>
        public int? ClassId { get; set; }

        /// <summary>
        /// Filter by date range start (UTC)
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Filter by date range end (UTC)
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Filter by disease type ID
        /// </summary>
        public int? DiseaseTypeId { get; set; }

        /// <summary>
        /// Filter by vaccination campaign ID
        /// </summary>
        public int? VaccinationCampaignId { get; set; }

        /// <summary>
        /// Filter by user status (ACTIVE, LOCKED, etc.)
        /// </summary>
        public string? UserStatus { get; set; }

        /// <summary>
        /// Filter by student health status (STABLE, FOLLOW_UP, HIGH_RISK)
        /// </summary>
        public string? HealthStatus { get; set; }

        /// <summary>
        /// Include low stock medicines in alerts
        /// </summary>
        public bool? IncludeLowStockMedicines { get; set; }

        /// <summary>
        /// Include expiring medicines in alerts
        /// </summary>
        public bool? IncludeExpiringMedicines { get; set; }
    }

    public class NurseDashboardOverviewDto
    {
        public int TotalVisitsToday { get; set; }
        public List<RecentExaminationDto> RecentExaminations { get; set; } = new();
        public List<MedicineAlertDto> LowStockMedicines { get; set; } = new();
        public List<MedicineAlertDto> ExpiringMedicines { get; set; } = new();
        public int PendingVaccinationsCount { get; set; }
    }

    public class MedicineAlertDto
    {
        public int MedicineId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public int WarningThreshold { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class VaccinationAlertDto
    {
        public int CampaignId { get; set; }
        public string CampaignName { get; set; } = string.Empty;
        public int PendingCount { get; set; }
    }

    public class RecentExaminationDto
    {
        public int VisitId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public DateTime VisitDate { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
    }
}