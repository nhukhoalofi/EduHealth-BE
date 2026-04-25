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

        Task<NurseReportDashboardDto> GetNurseDashboardAsync(string timeRange, DateTime? fromDate, DateTime? toDate, string? grade, string? classId, string? reportType, CancellationToken cancellationToken = default);
        Task<NurseClassDetailReportDto?> GetNurseClassDetailAsync(int classId, string timeRange, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
        Task<ExportFileDto> ExportNurseReportAsync(string format, string timeRange, DateTime? fromDate, DateTime? toDate, string? grade, string? classId, string? reportType, CancellationToken cancellationToken = default);

        Task<AdminNotificationPreviewResponseDto> PreviewNotificationsAsync(AdminNotificationPreviewRequestDto request, CancellationToken cancellationToken = default);
        Task<AdminNotificationResponseDto> SendNotificationsAsync(AdminNotificationRequestDto request, int adminId, CancellationToken cancellationToken = default);
    }
}