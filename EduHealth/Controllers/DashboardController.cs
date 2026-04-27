using EduHealth.DTOs.Common;
using EduHealth.DTOs.Dashboard;
using EduHealth.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHealth.Controllers
{
    [ApiController]
    [Route("api/v1/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ApiResponse<AdminDashboardOverviewDto>>> GetAdminDashboard()
        {
            var result = await _dashboardService.GetAdminOverviewAsync();
            return Ok(ApiResponse<AdminDashboardOverviewDto>.Ok(result, "Lấy thông tin tổng quan phòng Y Tế thành công."));
        }

        [HttpGet("nurse")]
        [Authorize(Roles = "NURSE")]
        public async Task<ActionResult<ApiResponse<NurseDashboardOverviewDto>>> GetNurseDashboard()
        {
            var result = await _dashboardService.GetNurseOverviewAsync();
            return Ok(ApiResponse<NurseDashboardOverviewDto>.Ok(result, "Lấy thông tin tổng quan Y tá thành công."));
        }

        [HttpGet("health-stats")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetHealthStats([FromQuery] int top = 5, CancellationToken cancellationToken = default)
        {
            var result = await _dashboardService.GetTopDiseasesAsync(top, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<TopDiseaseDto>>.Ok(result, "Lấy biểu đồ thống kê bệnh lý thành công."));
        }
    }
}