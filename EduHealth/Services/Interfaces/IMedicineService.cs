using EduHealth.DTOs.Medicines;

namespace EduHealth.Services.Interfaces
{
    public interface IMedicineService
    {
        Task<(IReadOnlyList<MedicineListItemDto> Items, int TotalItems, int TotalPages, int Page, int PageSize)> GetPagedAsync(MedicineListQueryDto query, CancellationToken cancellationToken = default);
        Task<(bool Found, MedicineDetailDto? Data)> GetDetailAsync(string id, CancellationToken cancellationToken = default);
        Task<(bool Success, int? StatusCode, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, MedicineDetailDto? Data)> CreateAsync(CreateMedicineRequestDto request, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, object? Data)> UpdateAsync(string id, UpdateMedicineRequestDto request, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, object? Data)> UpdateStatusAsync(string id, UpdateMedicineStatusRequestDto request, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, StockMovementResponseDto? Data)> StockInAsync(string id, int performedByUserId, StockInMedicineRequestDto request, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, StockMovementResponseDto? Data)> DisposeAsync(string id, int performedByUserId, DisposeMedicineRequestDto request, CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<MedicineMovementItemDto> Items, int TotalItems, int TotalPages, int Page, int PageSize)> GetMovementsAsync(string id, int page, int pageSize, string? type, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<MedicineAlertItemDto>> GetAlertsAsync(string type, CancellationToken cancellationToken = default);
    }
}
