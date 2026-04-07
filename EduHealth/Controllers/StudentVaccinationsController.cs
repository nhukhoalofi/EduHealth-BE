using EduHealth.DTOs.Common;
using EduHealth.DTOs.Vaccinations;
using EduHealth.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHealth.Controllers
{
    [ApiController]
    [Route("api/v1/student-vaccinations")]
    [Authorize]
    public class StudentVaccinationsController : ControllerBase
    {
        private readonly IVaccinationService _vaccinationService;

        public StudentVaccinationsController(IVaccinationService vaccinationService)
        {
            _vaccinationService = vaccinationService;
        }

        [HttpPatch("{id}")]
        [Authorize(Roles = "NURSE")]
        public async Task<IActionResult> Update([FromRoute] string id, [FromBody] UpdateStudentVaccinationRequestDto request, CancellationToken cancellationToken)
        {
            var (success, statusCode, message, errors, data) = await _vaccinationService.UpdateStudentVaccinationAsync(id, request, cancellationToken);

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

            return Ok(new ApiResponseV2<UpdateStudentVaccinationResponseDto>
            {
                Success = true,
                Message = message,
                Data = data,
                Meta = null,
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpGet("pending")]
        [Authorize(Roles = "NURSE,ADMIN")]
        public async Task<IActionResult> GetPending([FromQuery] PendingVaccinationsQueryDto query, CancellationToken cancellationToken)
        {
            var (items, totalItems, totalPages, page, pageSize) = await _vaccinationService.GetPendingAsync(query, cancellationToken);

            return Ok(new ApiResponseV2<IReadOnlyList<PendingVaccinationItemDto>>
            {
                Success = true,
                Message = "Lấy danh sách chưa hoàn thành tiêm thành công.",
                Data = items,
                Meta = new { page, pageSize, totalItems, totalPages },
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
