using EduHealth.DTOs.SystemLogs;

namespace EduHealth.Services.Interfaces
{
    public interface ISystemLogService
    {
        Task<(IReadOnlyList<SystemLogListItemDto> Items, int TotalItems, int TotalPages, int Page, int PageSize)> GetPagedAsync(SystemLogListQueryDto query, CancellationToken cancellationToken);
        Task<(bool Found, SystemLogDetailDto? Data)> GetDetailAsync(long id, CancellationToken cancellationToken);
    }
}
