namespace EduHealth.DTOs.Students.HealthProfile
{
    public class StudentHealthProfileResponseDto
    {
        public string StudentId { get; set; } = null!; // STDxxx
        public string StudentCode { get; set; } = null!; // HSxxx (username)
        public string FullName { get; set; } = null!;
        public string ClassId { get; set; } = null!; // CLSxxx
        public string ClassName { get; set; } = null!;
        public HealthProfileDto HealthProfile { get; set; } = null!;
    }

    public class HealthProfileDto
    {
        public float? HeightCm { get; set; }
        public float? WeightKg { get; set; }

        public string? BloodType { get; set; }
        public string? EyeStatus { get; set; }
        public string? ChronicNote { get; set; }
        public string? GeneralHealthNote { get; set; }

        public IReadOnlyList<StudentAllergyItemDto> Allergies { get; set; } = Array.Empty<StudentAllergyItemDto>();
        public UpdatedByDto? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class StudentAllergyItemDto
    {
        public string Id { get; set; } = null!; // SAxxx
        public string AllergyTypeId { get; set; } = null!; // ALGxxx
        public string AllergyTypeName { get; set; } = null!;
        public string? Note { get; set; }
    }

    public class UpdatedByDto
    {
        public string UserId { get; set; } = null!; // USRxxx
        public string FullName { get; set; } = null!;
    }

    public class UpdateStudentHealthProfileRequestDto
    {
        public float? HeightCm { get; set; }
        public float? WeightKg { get; set; }
        public string? BloodType { get; set; }
        public string? EyeStatus { get; set; }
        public string? ChronicNote { get; set; }
        public string? GeneralHealthNote { get; set; }
        public List<UpdateStudentAllergyItemDto>? Allergies { get; set; }
    }

    public class UpdateStudentAllergyItemDto
    {
        public int? AllergyId { get; set; }
        public string? AllergyTypeId { get; set; }
        public string? Note { get; set; }
    }

    public class AllergyTypeLookupItemDto
    {
        public int AllergyId { get; set; }
        public string AllergyTypeId { get; set; } = null!;
        public string AllergyTypeName { get; set; } = null!;
        public string? Severity { get; set; }
    }

    public class ClassGrowthComparisonResponseDto
    {
        public string ClassId { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public string Metric { get; set; } = null!;
        public string Unit { get; set; } = null!;
        public ClassGrowthStudentPointDto CurrentStudent { get; set; } = null!;
        public IReadOnlyList<ClassGrowthStudentPointDto> Students { get; set; } = Array.Empty<ClassGrowthStudentPointDto>();
        public ClassGrowthSummaryDto Summary { get; set; } = null!;
    }

    public class ClassGrowthStudentPointDto
    {
        public string StudentId { get; set; } = null!;
        public string StudentCode { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public float Value { get; set; }
        public int Rank { get; set; }
        public bool IsCurrentStudent { get; set; }
    }

    public class ClassGrowthSummaryDto
    {
        public int TotalStudents { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
        public float Average { get; set; }
        public float CurrentValue { get; set; }
        public float Percentile { get; set; }
    }
}
