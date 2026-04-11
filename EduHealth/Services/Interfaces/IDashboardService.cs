using EduHealth.DTOs.Dashboard;

namespace EduHealth.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TopDiseaseDto>> GetTopDiseasesAsync(int top = 5, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RecentVisitDto>> GetRecentVisitsAsync(int count = 5, CancellationToken cancellationToken = default);
    }
}