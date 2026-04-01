using EduHealth.DTOs.Common;
using EduHealth.DTOs.Examinations;
using EduHealth.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduHealth.Controllers
{
    [ApiController]
    [Route("api/v1/examinations")]
    [Authorize]
    public class ExaminationsController : ControllerBase
    {
        private readonly IExaminationService _examinationService;

        public ExaminationsController(IExaminationService examinationService)
        {
            _examinationService = examinationService;
        }

        [HttpGet]
        [Authorize(Roles = "NURSE,ADMIN")]
        public async Task<IActionResult> GetPaged([FromQuery] ExaminationListQueryDto query, CancellationToken cancellationToken)
        {
            var (items, totalItems, totalPages, page, pageSize) = await _examinationService.GetPagedAsync(query, cancellationToken);

            return Ok(new ApiResponseV2<IReadOnlyList<ExaminationListItemDto>>
            {
                Success = true,
                Message = "Lấy danh sách phiếu khám thành công.",
                Data = items,
                Meta = new { page, pageSize, totalItems, totalPages },
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpPost]
        [Authorize(Roles = "NURSE")]
        public async Task<IActionResult> Create([FromBody] CreateExaminationRequestDto request, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _ = int.TryParse(userIdClaim, out var nurseUserId);

            var (success, statusCode, message, errors, data) = await _examinationService.CreateAsync(nurseUserId, request, cancellationToken);

            if (!success)
            {
                return StatusCode(statusCode ?? 400, new ApiErrorResponseV2
                {
                    Success = false,
                    Message = message,
                    Errors = errors.Select(x => new ApiErrorItemDto { Field = x.Field, Code = x.Code, Message = x.Message }).ToList(),
                    Timestamp = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return StatusCode(201, new ApiResponseV2<CreateExaminationResponseDto>
            {
                Success = true,
                Message = message,
                Data = data,
                Meta = null,
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "NURSE,ADMIN")]
        public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken cancellationToken)
        {
            var data = await _examinationService.GetByIdAsync(id, cancellationToken);

            if (data is null)
            {
                return NotFound(new ApiErrorResponseV2
                {
                    Success = false,
                    Message = "Không tìm thấy phiếu khám.",
                    Errors = new[] { new ApiErrorItemDto { Field = "id", Code = "EXAMINATION_NOT_FOUND", Message = "Không tồn tại phiếu khám với id đã cung cấp." } },
                    Timestamp = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(new ApiResponseV2<ExaminationDetailDto>
            {
                Success = true,
                Message = "Lấy chi tiết phiếu khám thành công.",
                Data = data,
                Meta = null,
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
