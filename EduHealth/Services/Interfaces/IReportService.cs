using EduHealth.DTOs.Reports;
using EduHealth.DTOs.Dashboard;

namespace EduHealth.Services.Interfaces
{
    public interface IReportService
    {
        Task<AdminReportDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken);
        Task<AdminReportDashboardDto> GetAdminDashboardAsync(AdminDashboardFilterDto filter, CancellationToken cancellationToken);
        Task<AdminClassDetailDto?> GetAdminClassDetailAsync(int classId, CancellationToken cancellationToken);
        Task<ReportClassDto?> GetClassReportAsync(int classId, CancellationToken cancellationToken);
        Task<ExportFileDto> ExportReportXlsxAsync(ExportRequestDto request, CancellationToken cancellationToken);
        Task<DirectiveResponseDto> CreateDirectiveAsync(DirectiveRequestDto request, int adminUserId, CancellationToken cancellationToken);
        Task<SystemLogSummaryDto> GetSystemLogSummaryAsync(CancellationToken cancellationToken);
        Task<NurseReportDashboardDto> GetNurseDashboardAsync(int nurseId, CancellationToken cancellationToken);

        Task<AdminNotificationPreviewResponseDto> PreviewNotificationsAsync(AdminNotificationPreviewRequestDto request, CancellationToken cancellationToken);
        Task<AdminNotificationResponseDto> SendNotificationsAsync(AdminNotificationRequestDto request, int adminId, CancellationToken cancellationToken);
    }
}