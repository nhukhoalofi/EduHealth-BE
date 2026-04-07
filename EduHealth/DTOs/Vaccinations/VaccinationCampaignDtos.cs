namespace EduHealth.DTOs.Vaccinations
{
    public class VaccinationCampaignListQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Keyword { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public string? ClassId { get; set; } // CLSxxx
        public string? Status { get; set; }
    }

    public class CampaignStatisticsDto
    {
        public int TotalStudents { get; set; }
        public int DoneCount { get; set; }
        public int PendingCount { get; set; }
        public int PostponedCount { get; set; }
        public int ContraindicatedCount { get; set; }
        public int AbsentCount { get; set; }
    }

    public class VaccinationCampaignListItemDto
    {
        public string Id { get; set; } = null!; // VACxxx
        public string Name { get; set; } = null!;
        public string VaccineName { get; set; } = null!;
        public int DoseNumber { get; set; }
        public DateOnly ScheduledDate { get; set; }
        public string TargetType { get; set; } = null!;
        public string Status { get; set; } = null!;
        public CampaignStatisticsDto Statistics { get; set; } = new();
    }

    public class CreateVaccinationCampaignRequestDto
    {
        public string Name { get; set; } = null!;
        public string VaccineName { get; set; } = null!;
        public int DoseNumber { get; set; }
        public DateOnly ScheduledDate { get; set; }
        public string TargetType { get; set; } = null!; // CLASS | STUDENT
        public List<string>? TargetClassIds { get; set; }
        public List<int>? TargetStudentIds { get; set; } // student userId
        public string? Note { get; set; }
    }

    public class CreateVaccinationCampaignResponseDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string VaccineName { get; set; } = null!;
        public int DoseNumber { get; set; }
        public DateOnly ScheduledDate { get; set; }
        public string TargetType { get; set; } = null!;
        public IReadOnlyList<string> TargetClassIds { get; set; } = Array.Empty<string>();
        public int GeneratedStudentRecords { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class VaccinationCampaignDetailDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string VaccineName { get; set; } = null!;
        public int DoseNumber { get; set; }
        public DateOnly ScheduledDate { get; set; }
        public string TargetType { get; set; } = null!;
        public IReadOnlyList<string> TargetClassIds { get; set; } = Array.Empty<string>();
        public string? Note { get; set; }
        public string Status { get; set; } = null!;
        public CampaignStatisticsDto Statistics { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class CampaignStudentListQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Status { get; set; }
        public string? Keyword { get; set; }
    }

    public class CampaignStudentItemDto
    {
        public string StudentVaccinationId { get; set; } = null!; // SVxxx
        public CampaignStudentBriefDto Student { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateOnly? VaccinatedAt { get; set; }
        public string? LotNumber { get; set; }
        public string? Note { get; set; }
    }

    public class CampaignStudentBriefDto
    {
        public string StudentId { get; set; } = null!; // STDxxx
        public string StudentCode { get; set; } = null!; // HSxxx
        public string FullName { get; set; } = null!;
        public string ClassId { get; set; } = null!;
        public string ClassName { get; set; } = null!;
    }

    public class UpdateStudentVaccinationRequestDto
    {
        public string Status { get; set; } = null!; // PENDING, DONE, POSTPONED, CONTRAINDICATED, ABSENT
        public DateOnly? VaccinatedAt { get; set; }
        public string? LotNumber { get; set; }
        public string? Note { get; set; }
    }

    public class UpdateStudentVaccinationResponseDto
    {
        public string StudentVaccinationId { get; set; } = null!;
        public string CampaignId { get; set; } = null!;
        public string StudentId { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateOnly? VaccinatedAt { get; set; }
        public string? LotNumber { get; set; }
        public string? Note { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class PendingVaccinationsQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? CampaignId { get; set; }
        public string? ClassId { get; set; }
    }

    public class PendingVaccinationItemDto
    {
        public string StudentVaccinationId { get; set; } = null!;
        public string CampaignId { get; set; } = null!;
        public string CampaignName { get; set; } = null!;
        public CampaignStudentBriefDto Student { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateOnly ScheduledDate { get; set; }
    }

    public class StudentVaccinationHistoryItemDto
    {
        public string StudentVaccinationId { get; set; } = null!;
        public string CampaignId { get; set; } = null!;
        public string CampaignName { get; set; } = null!;
        public string VaccineName { get; set; } = null!;
        public int DoseNumber { get; set; }
        public DateOnly ScheduledDate { get; set; }
        public string Status { get; set; } = null!;
        public DateOnly? VaccinatedAt { get; set; }
        public string? LotNumber { get; set; }
        public string? Note { get; set; }
    }
}
