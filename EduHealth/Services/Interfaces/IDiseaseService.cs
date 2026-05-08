using EduHealth.DTOs.Diseases;

namespace EduHealth.Services.Interfaces
{
    public interface IDiseaseService
    {
        Task<IReadOnlyList<DiseaseListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<(bool Success, int? StatusCode, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, DiseaseDetailDto? Data)> CreateAsync(
            CreateDiseaseRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
