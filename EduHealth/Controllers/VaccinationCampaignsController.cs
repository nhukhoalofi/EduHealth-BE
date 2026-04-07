using EduHealth.DTOs.Common;
using EduHealth.DTOs.Vaccinations;
using EduHealth.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduHealth.Controllers
{
    [ApiController]
    [Route("api/v1/vaccination-campaigns")]
    [Authorize]
    public class VaccinationCampaignsController : ControllerBase
    {
        private readonly IVaccinationService _vaccinationService;

        public VaccinationCampaignsController(IVaccinationService vaccinationService)
        {
            _vaccinationService = vaccinationService;
        }

        [HttpGet]
        [Authorize(Roles = "NURSE,ADMIN")]
        public async Task<IActionResult> GetPaged([FromQuery] VaccinationCampaignListQueryDto query, CancellationToken cancellationToken)
        {
            var (items, totalItems, totalPages, page, pageSize) = await _vaccinationService.GetCampaignsAsync(query, cancellationToken);

            return Ok(new ApiResponseV2<IReadOnlyList<VaccinationCampaignListItemDto>>
            {
                Success = true,
                Message = "Lấy danh sách đợt tiêm thành công.",
                Data = items,
                Meta = new { page, pageSize, totalItems, totalPages },
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpPost]
        [Authorize(Roles = "NURSE,ADMIN")]
        public async Task<IActionResult> Create([FromBody] CreateVaccinationCampaignRequestDto request, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _ = int.TryParse(userIdClaim, out var userId);

            var (success, statusCode, message, errors, data) = await _vaccinationService.CreateCampaignAsync(userId, request, cancellationToken);

            if (!success)
            {
                return StatusCode(statusCode, new ApiErrorResponseV2
                {
                    Success = false,
                    Message = message,
                    Errors = errors.Select(x => new ApiErrorItemDto { Field = x.Field, Code = x.Code, Message = x.Message }).ToList(),
                    Timestamp = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return StatusCode(201, new ApiResponseV2<CreateVaccinationCampaignResponseDto>
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
        public async Task<IActionResult> GetDetail([FromRoute] string id, CancellationToken cancellationToken)
        {
            var data = await _vaccinationService.GetCampaignDetailAsync(id, cancellationToken);

            if (data is null)
            {
                return NotFound(new ApiErrorResponseV2
                {
                    Success = false,
                    Message = "Không tìm thấy đợt tiêm.",
                    Errors = new[] { new ApiErrorItemDto { Field = "id", Code = "VACCINATION_CAMPAIGN_NOT_FOUND", Message = "Không tồn tại đợt tiêm với id đã cung cấp." } },
                    Timestamp = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(new ApiResponseV2<VaccinationCampaignDetailDto>
            {
                Success = true,
                Message = "Lấy chi tiết đợt tiêm thành công.",
                Data = data,
                Meta = null,
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpGet("{id}/students")]
        [Authorize(Roles = "NURSE")]
        public async Task<IActionResult> GetStudents([FromRoute] string id, [FromQuery] CampaignStudentListQueryDto query, CancellationToken cancellationToken)
        {
            var result = await _vaccinationService.GetCampaignStudentsAsync(id, query, cancellationToken);

            if (result is null)
            {
                return NotFound(new ApiErrorResponseV2
                {
                    Success = false,
                    Message = "Không tìm thấy đợt tiêm.",
                    Errors = new[] { new ApiErrorItemDto { Field = "id", Code = "VACCINATION_CAMPAIGN_NOT_FOUND", Message = "Không tồn tại đợt tiêm với id đã cung cấp." } },
                    Timestamp = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var (items, totalItems, totalPages, page, pageSize) = result.Value;
            return Ok(new ApiResponseV2<IReadOnlyList<CampaignStudentItemDto>>
            {
                Success = true,
                Message = "Lấy danh sách học sinh trong đợt tiêm thành công.",
                Data = items,
                Meta = new { page, pageSize, totalItems, totalPages },
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
