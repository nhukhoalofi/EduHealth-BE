using EduHealth.DTOs.Common;
using EduHealth.DTOs.Dashboard;
using EduHealth.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHealth.Controllers
{
    [ApiController]
    [Route("api/v1/dashboard")]
    [Authorize(Roles = "ADMIN,NURSE")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
        {
            var result = await _dashboardService.GetSummaryAsync(cancellationToken);
            return Ok(ApiResponse<DashboardSummaryDto>.Ok(result, "Lấy thông tin tổng quan thành công."));
        }

        [HttpGet("health-stats")]
        public async Task<IActionResult> GetHealthStats([FromQuery] int top = 5, CancellationToken cancellationToken = default)
        {
            var result = await _dashboardService.GetTopDiseasesAsync(top, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<TopDiseaseDto>>.Ok(result, "Lấy biểu đồ sức khỏe thành công."));
        }

        [HttpGet("recent-examinations")]
        public async Task<IActionResult> GetRecentVisits([FromQuery] int count = 5, CancellationToken cancellationToken = default)
        {
            var result = await _dashboardService.GetRecentVisitsAsync(count, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<RecentVisitDto>>.Ok(result, "Lấy danh sách khám bệnh gần đây thành công."));
        }
    }
}