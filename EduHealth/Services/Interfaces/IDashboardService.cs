using EduHealth.DTOs.Dashboard;

namespace EduHealth.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<AdminDashboardOverviewDto> GetAdminOverviewAsync();
        Task<NurseDashboardOverviewDto> GetNurseOverviewAsync();
        Task<IReadOnlyList<TopDiseaseDto>> GetTopDiseasesAsync(int top = 5, CancellationToken cancellationToken = default);
    }
}