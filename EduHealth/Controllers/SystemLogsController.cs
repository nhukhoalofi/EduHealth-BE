using EduHealth.DTOs.Common;
using EduHealth.DTOs.SystemLogs;
using EduHealth.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHealth.Controllers
{
    [ApiController]
    [Route("api/v1/system-logs")]
    [Authorize(Roles = "ADMIN")]
    public sealed class SystemLogsController : ControllerBase
    {
        private readonly ISystemLogService _service;

        public SystemLogsController(ISystemLogService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] SystemLogListQueryDto query, CancellationToken cancellationToken)
        {
            var (items, totalItems, totalPages, page, pageSize) = await _service.GetPagedAsync(query, cancellationToken);

            return Ok(new ApiResponseV2<IReadOnlyList<SystemLogListItemDto>>
            {
                Success = true,
                Message = "Fetched successfully",
                Data = items,
                Meta = new { page, pageSize, totalItems, totalPages },
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetDetail([FromRoute] long id, CancellationToken cancellationToken)
        {
            var (found, data) = await _service.GetDetailAsync(id, cancellationToken);

            if (!found)
            {
                return NotFound(new ApiErrorResponseV2
                {
                    Success = false,
                    Message = "Không tìm thấy log.",
                    Errors = new[] { new ApiErrorItemDto { Field = "id", Code = "SYSTEM_LOG_NOT_FOUND", Message = "Không tồn tại system log với id đã cung cấp." } },
                    Timestamp = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(new ApiResponseV2<SystemLogDetailDto>
            {
                Success = true,
                Message = "Fetched successfully",
                Data = data,
                Meta = null,
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
