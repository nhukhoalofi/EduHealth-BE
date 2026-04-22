using EduHealth.DTOs.Common;
using EduHealth.DTOs.Reports;
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

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        // ---------- ADMIN REPORTS ---------- //

        [HttpGet("admin/dashboard")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetAdminDashboard(CancellationToken cancellationToken)
        {
            var data = await _reportService.GetAdminDashboardAsync(cancellationToken);
            return Ok(new ApiResponseV2<AdminReportDashboardDto>
            {
                Success = true, Message = "Lấy dữ liệu dashboard admin thành công", Data = data, Timestamp = DateTime.UtcNow, TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpGet("admin/classes/{classId}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetAdminClassReport([FromRoute] int classId, CancellationToken cancellationToken)
        {
            var data = await _reportService.GetClassReportAsync(classId, cancellationToken);
            if (data == null) 
                return NotFound(new ApiErrorResponseV2 { Success = false, Message = "Không tìm thấy lớp học.", Timestamp = DateTime.UtcNow, TraceId = HttpContext.TraceIdentifier });

            return Ok(new ApiResponseV2<ReportClassDto>
            {
                Success = true, Message = "Lấy dữ liệu báo cáo lớp thành công", Data = data, Timestamp = DateTime.UtcNow, TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpGet("admin/export")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> ExportAdminReport([FromQuery] ExportRequestDto request, CancellationToken cancellationToken)
        {
            var data = await _reportService.ExportReportAsync(request, cancellationToken);
            return Ok(new ApiResponseV2<ExportResponseDto> { Success = true, Message = "Xuất báo cáo thành công", Data = data, Timestamp = DateTime.UtcNow });
        }

        [HttpPost("admin/directives")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> CreateDirective([FromBody] DirectiveRequestDto request, CancellationToken cancellationToken)
        {
            var data = await _reportService.CreateDirectiveAsync(request, cancellationToken);
            return Ok(new ApiResponseV2<DirectiveResponseDto> { Success = true, Message = "Tạo chỉ đạo thành công", Data = data, Timestamp = DateTime.UtcNow });
        }

        [HttpGet("admin/system-logs/summary")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetSystemLogSummary(CancellationToken cancellationToken)
        {
            var data = await _reportService.GetSystemLogSummaryAsync(cancellationToken);
            return Ok(new ApiResponseV2<SystemLogSummaryDto> { Success = true, Message = "Lấy dữ liệu log hệ thống thành công", Data = data, Timestamp = DateTime.UtcNow });
        }

        [HttpPost("admin/notifications/preview")]
        [Authorize(Roles = "ADMIN")]
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

        [HttpPost("admin/notifications")]
        [Authorize(Roles = "ADMIN")]
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

        // ---------- NURSE REPORTS ---------- //

        [HttpGet("nurse/dashboard")]
        [Authorize(Roles = "NURSE")]
        public async Task<IActionResult> GetNurseDashboard(CancellationToken cancellationToken)
        {
            var data = await _reportService.GetNurseDashboardAsync(0, cancellationToken); // Truyền stub NurseId
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
            var data = await _reportService.ExportReportAsync(request, cancellationToken);
            return Ok(new ApiResponseV2<ExportResponseDto> { Success = true, Message = "Xuất báo cáo thành công", Data = data, Timestamp = DateTime.UtcNow });
        }
    }
}