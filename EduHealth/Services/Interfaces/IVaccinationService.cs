using EduHealth.DTOs.Vaccinations;

namespace EduHealth.Services.Interfaces
{
    public interface IVaccinationService
    {
        Task<(IReadOnlyList<VaccinationCampaignListItemDto> Items, int TotalItems, int TotalPages, int Page, int PageSize)> GetCampaignsAsync(
            VaccinationCampaignListQueryDto query,
            CancellationToken cancellationToken = default);

        Task<(bool Success, int StatusCode, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, CreateVaccinationCampaignResponseDto? Data)> CreateCampaignAsync(
            int createdByUserId,
            CreateVaccinationCampaignRequestDto request,
            CancellationToken cancellationToken = default);

        Task<VaccinationCampaignDetailDto?> GetCampaignDetailAsync(string id, CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<CampaignStudentItemDto> Items, int TotalItems, int TotalPages, int Page, int PageSize)?> GetCampaignStudentsAsync(
            string campaignId,
            CampaignStudentListQueryDto query,
            CancellationToken cancellationToken = default);

        Task<(bool Success, int StatusCode, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, UpdateStudentVaccinationResponseDto? Data)> UpdateStudentVaccinationAsync(
            string studentVaccinationId,
            UpdateStudentVaccinationRequestDto request,
            CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<PendingVaccinationItemDto> Items, int TotalItems, int TotalPages, int Page, int PageSize)> GetPendingAsync(
            PendingVaccinationsQueryDto query,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StudentVaccinationHistoryItemDto>?> GetStudentVaccinationHistoryAsync(
            int studentUserId,
            CancellationToken cancellationToken = default);
    }
}
