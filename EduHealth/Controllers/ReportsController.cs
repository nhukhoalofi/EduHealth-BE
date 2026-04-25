using EduHealth.DTOs.Common;
using EduHealth.DTOs.Reports;
using EduHealth.DTOs.Dashboard;
using EduHealth.DTOs.Notifications;
using EduHealth.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduHealth.Controllers
{
    [ApiController]
    [Route("api/v1/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly INotificationService _notificationService;

        public ReportsController(IReportService reportService, INotificationService notificationService)
        {
            _reportService = reportService;
            _notificationService = notificationService;
        }

        [HttpGet("admin/dashboard")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetAdminDashboard(
            [FromQuery] int? classId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int? diseaseTypeId,
            [FromQuery] int? vaccinationCampaignId,
            [FromQuery] string? userStatus,
            [FromQuery] string? healthStatus,
            [FromQuery] bool? includeLowStockMedicines,
            [FromQuery] bool? includeExpiringMedicines,
            CancellationToken cancellationToken)
        {
            var filter = new AdminDashboardFilterDto
            {
                ClassId = classId,
                FromDate = fromDate,
                ToDate = toDate,
                DiseaseTypeId = diseaseTypeId,
                VaccinationCampaignId = vaccinationCampaignId,
                UserStatus = userStatus,
                HealthStatus = healthStatus,
                IncludeLowStockMedicines = includeLowStockMedicines,
                IncludeExpiringMedicines = includeExpiringMedicines
            };

            var data = await _reportService.GetAdminDashboardAsync(filter, cancellationToken);
            return Ok(new ApiResponseV2<AdminReportDashboardDto>
            {
                Success = true,
                Message = "Lấy dữ liệu dashboard admin thành công",
                Data = data,
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpGet("admin/classes/{classId}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetAdminClassReport([FromRoute] int classId, CancellationToken cancellationToken)
        {
            var detail = await _reportService.GetAdminClassDetailAsync(classId, cancellationToken);
            if (detail == null)
            {
                return NotFound(new ApiErrorResponseV2
                {
                    Success = false,
                    Message = "Không tìm thấy lớp học.",
                    Timestamp = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(new ApiResponseV2<AdminClassDetailEnvelopeDto>
            {
                Success = true,
                Message = "Lấy dữ liệu báo cáo lớp thành công",
                Data = new AdminClassDetailEnvelopeDto { Detail = detail },
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpGet("admin/export")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> ExportAdminReport([FromQuery] ExportRequestDto request, CancellationToken cancellationToken)
        {
            var file = await _reportService.ExportReportXlsxAsync(request, cancellationToken);
            return File(file.FileBytes, file.ContentType, file.FileName);
        }

        [HttpPost("admin/directives")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> CreateDirective([FromBody] DirectiveRequestDto request, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _ = int.TryParse(userIdClaim, out var adminUserId);

            // Build notification request
            var notificationRequest = new CreateNotificationRequestDto
            {
                Title = request.Title,
                Content = request.Content,
                Type = "DIRECTIVE",
                ClassId = request.ClassId,
                DiseaseId = null,
                VaccinationId = null,
                RecipientUserIds = request.RecipientUserIds ?? new List<int>()
            };

            var result = await _notificationService.CreateAsync(adminUserId, notificationRequest, cancellationToken);
            
            return Ok(new ApiResponseV2<CreateNotificationResponseDto>
            {
                Success = true,
                Message = "Tạo chỉ thị thành công",
                Data = result,
                Timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("admin/system-logs/summary")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetSystemLogSummary(CancellationToken cancellationToken)
        {
            var data = await _reportService.GetSystemLogSummaryAsync(cancellationToken);
            return Ok(new ApiResponseV2<SystemLogSummaryDto> { Success = true, Message = "Lấy dữ liệu log hệ thống thành công", Data = data, Timestamp = DateTime.UtcNow });
        }

        // Deprecated: Use POST /api/v1/notifications/recipients/preview instead
        [HttpPost("admin/notifications/preview")]
        [Authorize(Roles = "ADMIN")]
        [Obsolete("Use POST /api/v1/notifications/recipients/preview instead", false)]
        public async Task<IActionResult> PreviewAdminNotifications([FromBody] AdminNotificationPreviewRequestDto request, CancellationToken cancellationToken)
        {
            if (request.ClassId == null && (request.RecipientUserIds == null || !request.RecipientUserIds.Any()))
            {
                return BadRequest(new ApiErrorResponseV2
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ.",
                    Errors = new[] { new ApiErrorItemDto { Field = "target", Code = "MISSING_TARGET", Message = "Phải cung cấp ClassId hoặc RecipientUserIds." } },
                    Timestamp = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var data = await _reportService.PreviewNotificationsAsync(request, cancellationToken);
            return Ok(new ApiResponseV2<AdminNotificationPreviewResponseDto> { Success = true, Message = "Lấy dữ liệu preview thành công.", Data = data, Timestamp = DateTime.UtcNow });
        }

        // Deprecated: Use POST /api/v1/notifications instead
        [HttpPost("admin/notifications")]
        [Authorize(Roles = "ADMIN")]
        [Obsolete("Use POST /api/v1/notifications 대신", false)]
        public async Task<IActionResult> SendAdminNotifications([FromBody] AdminNotificationRequestDto request, CancellationToken cancellationToken)
        {
            if (request.ClassId == null && (request.RecipientUserIds == null || !request.RecipientUserIds.Any()))
            {
                return BadRequest(new ApiErrorResponseV2
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ.",
                    Errors = new[] { new ApiErrorItemDto { Field = "target", Code = "MISSING_TARGET", Message = "Phải cung cấp ClassId hoặc RecipientUserIds." } },
                    Timestamp = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var rootUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _ = int.TryParse(rootUserIdClaim, out var adminId);

            var data = await _reportService.SendNotificationsAsync(request, adminId, cancellationToken);
            return Ok(new ApiResponseV2<AdminNotificationResponseDto> { Success = true, Message = "Gửi thông báo thành công.", Data = data, Timestamp = DateTime.UtcNow });
        }

        [HttpGet("nurse/dashboard")]
        [Authorize(Roles = "NURSE")]
        public async Task<IActionResult> GetNurseDashboard(CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _ = int.TryParse(userIdClaim, out var nurseId);
            var data = await _reportService.GetNurseDashboardAsync(nurseId, cancellationToken);
            return Ok(new ApiResponseV2<NurseReportDashboardDto> { Success = true, Message = "Lấy dữ liệu dashboard Y tá thành công", Data = data, Timestamp = DateTime.UtcNow });
        }

        [HttpGet("nurse/classes/{classId}")]
        [Authorize(Roles = "NURSE")]
        public async Task<IActionResult> GetNurseClassReport([FromRoute] int classId, CancellationToken cancellationToken)
        {
            var data = await _reportService.GetClassReportAsync(classId, cancellationToken);
            if (data == null)
                return NotFound(new ApiErrorResponseV2 { Success = false, Message = "Không tìm thấy lớp học.", Timestamp = DateTime.UtcNow, TraceId = HttpContext.TraceIdentifier });

            return Ok(new ApiResponseV2<ReportClassDto> { Success = true, Message = "Lấy dữ liệu báo cáo lớp thành công", Data = data, Timestamp = DateTime.UtcNow });
        }

        [HttpGet("nurse/export")]
        [Authorize(Roles = "NURSE")]
        public async Task<IActionResult> ExportNurseReport([FromQuery] ExportRequestDto request, CancellationToken cancellationToken)
        {
            var file = await _reportService.ExportReportXlsxAsync(request, cancellationToken);
            return File(file.FileBytes, file.ContentType, file.FileName);
        }
    }
}