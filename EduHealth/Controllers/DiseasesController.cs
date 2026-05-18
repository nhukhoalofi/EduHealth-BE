using EduHealth.DTOs.Common;
using EduHealth.DTOs.Diseases;
using EduHealth.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHealth.Controllers
{
    [ApiController]
    [Route("api/v1/diseases")]
    [Authorize]
    public sealed class DiseasesController : ControllerBase
    {
        private readonly IDiseaseService _diseaseService;

        public DiseasesController(IDiseaseService diseaseService)
        {
            _diseaseService = diseaseService;
        }

        [HttpGet]
        [Authorize(Roles = "ADMIN,NURSE")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var data = await _diseaseService.GetAllAsync(cancellationToken);

            return Ok(new ApiResponseV2<IReadOnlyList<DiseaseListItemDto>>
            {
                Success = true,
                Message = "Lấy danh sách loại bệnh thành công.",
                Data = data,
                Meta = null,
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,NURSE")]
        public async Task<IActionResult> Create([FromBody] CreateDiseaseRequestDto request, CancellationToken cancellationToken)
        {
            var (success, statusCode, message, errors, data) = await _diseaseService.CreateAsync(request, cancellationToken);

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

            return StatusCode(201, new ApiResponseV2<DiseaseDetailDto>
            {
                Success = true,
                Message = message,
                Data = data,
                Meta = null,
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
