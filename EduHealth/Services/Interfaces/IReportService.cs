using EduHealth.DTOs.Reports;

namespace EduHealth.Services.Interfaces
{
    public interface IReportService
    {
        Task<AdminReportDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken);
        Task<ReportClassDto?> GetClassReportAsync(int classId, CancellationToken cancellationToken);
        Task<ExportResponseDto> ExportReportAsync(ExportRequestDto request, CancellationToken cancellationToken);
        Task<DirectiveResponseDto> CreateDirectiveAsync(DirectiveRequestDto request, CancellationToken cancellationToken);
        Task<SystemLogSummaryDto> GetSystemLogSummaryAsync(CancellationToken cancellationToken);
        
        Task<NurseReportDashboardDto> GetNurseDashboardAsync(int nurseId, CancellationToken cancellationToken);

        Task<AdminNotificationPreviewResponseDto> PreviewNotificationsAsync(AdminNotificationPreviewRequestDto request, CancellationToken cancellationToken);
        Task<AdminNotificationResponseDto> SendNotificationsAsync(AdminNotificationRequestDto request, int adminId, CancellationToken cancellationToken);
    }
}