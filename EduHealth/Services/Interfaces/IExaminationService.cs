using EduHealth.DTOs.Examinations;

namespace EduHealth.Services.Interfaces
{
    public interface IExaminationService
    {
        Task<(IReadOnlyList<ExaminationListItemDto> Items, int TotalItems, int TotalPages, int Page, int PageSize)> GetPagedAsync(ExaminationListQueryDto query, CancellationToken cancellationToken = default);
        Task<ExaminationDetailDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<(bool Success, int? StatusCode, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, CreateExaminationResponseDto? Data)> CreateAsync(int nurseUserId, CreateExaminationRequestDto request, CancellationToken cancellationToken = default);
    }
}
