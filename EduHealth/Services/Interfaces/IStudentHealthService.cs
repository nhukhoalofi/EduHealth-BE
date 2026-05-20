using EduHealth.DTOs.Students.HealthProfile;

namespace EduHealth.Services.Interfaces
{
    public interface IStudentHealthService
    {
        Task<IReadOnlyList<AllergyTypeLookupItemDto>> GetAllergyTypesAsync(CancellationToken cancellationToken = default);
        Task<StudentHealthProfileResponseDto?> GetHealthProfileAsync(int studentUserId, CancellationToken cancellationToken = default);
        Task<ClassGrowthComparisonResponseDto?> GetClassGrowthComparisonAsync(
            int studentUserId,
            string? metric,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string Message, string? Field, StudentHealthProfileResponseDto? Data)> UpdateHealthProfileAsync(
            int nurseUserId,
            int studentUserId,
            UpdateStudentHealthProfileRequestDto request,
            CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<StudentHealthHistoryItemDto> Items, int TotalItems, int TotalPages, int Page, int PageSize)?> GetHealthHistoryAsync(
            int studentUserId,
            StudentHealthHistoryQueryDto query,
            CancellationToken cancellationToken = default);
    }
}
