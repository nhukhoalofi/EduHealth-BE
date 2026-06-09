using EduHealth.DTOs.Common;
using EduHealth.DTOs.Students;
using EduHealth.DTOs.Students.HealthProfile;
using EduHealth.DTOs.Vaccinations;
using EduHealth.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduHealth.Controllers
{
    [ApiController]
    [Route("api/v1/students")]
    [Authorize]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly IStudentHealthService _studentHealthService;
        private readonly IVaccinationService _vaccinationService;

        public StudentsController(IStudentService studentService, IStudentHealthService studentHealthService, IVaccinationService vaccinationService)
        {
            _studentService = studentService;
            _studentHealthService = studentHealthService;
            _vaccinationService = vaccinationService;
        }

        [HttpGet("allergy-types")]
        [Authorize(Roles = "ADMIN,NURSE")]
        public async Task<IActionResult> GetAllergyTypes(CancellationToken cancellationToken)
        {
            var data = await _studentHealthService.GetAllergyTypesAsync(cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<AllergyTypeLookupItemDto>>.Ok(data, "Lấy danh mục dị ứng thành công."));
        }

        [HttpGet]
        [Authorize(Roles = "ADMIN,NURSE")]
        public async Task<IActionResult> GetStudents([FromQuery] StudentListQueryDto query, CancellationToken cancellationToken)
        {
            var result = await _studentService.GetStudentsAsync(query, cancellationToken);

            return Ok(new ApiResponse<IReadOnlyList<StudentListItemDto>>
            {
                Success = true,
                Message = "Lấy danh sách học sinh thành công.",
                Data = result.Items,
                Meta = new
                {
                    page = result.Page,
                    pageSize = result.PageSize,
                    total = result.Total,
                    totalPages = result.TotalPages
                }
            });
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> CreateStudent([FromBody] StudentCreateRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _studentService.CreateStudentAsync(request, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.Fail(result.Message, result.Field));
            }

            return Ok(ApiResponse<StudentDetailDto>.Ok(result.Data, "Tạo hồ sơ học sinh thành công."));
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "ADMIN,NURSE")]
        public async Task<IActionResult> GetStudentById([FromRoute] int id, CancellationToken cancellationToken)
        {
            var result = await _studentService.GetStudentByIdAsync(id, cancellationToken);

            if (result is null)
            {
                return NotFound(ApiResponse<object>.Fail("Không tìm thấy học sinh."));
            }

            return Ok(ApiResponse<StudentDetailDto>.Ok(result, "Lấy chi tiết học sinh thành công."));
        }

        [HttpPatch("{id:int}")]
        [Authorize(Roles = "ADMIN,NURSE")]
        public async Task<IActionResult> UpdateStudent([FromRoute] int id, [FromBody] StudentUpdateRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _studentService.UpdateStudentAsync(id, request, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.Fail(result.Message, result.Field));
            }

            return Ok(ApiResponse<object>.Ok(null, result.Message));
        }

        [HttpPatch("{id:int}/image")]
        [Authorize(Roles = "ADMIN")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateStudentImage(
            [FromRoute] int id,
            [FromForm] StudentImageUpdateRequestDto request,
            CancellationToken cancellationToken)
        {
            var result = await _studentService.UpdateStudentImageAsync(id, request.File, cancellationToken);

            if (!result.Success)
            {
                if (result.Field == "id")
                {
                    return NotFound(ApiResponse<object>.Fail(result.Message, result.Field));
                }

                return BadRequest(ApiResponse<object>.Fail(result.Message, result.Field));
            }

            return Ok(ApiResponse<object>.Ok(new { imageUrl = result.ImageUrl }, result.Message));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> DeleteStudent([FromRoute] int id, CancellationToken cancellationToken)
        {
            var result = await _studentService.DeleteStudentAsync(id, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.Fail(result.Message, result.Field));
            }

            return Ok(ApiResponse<object>.Ok(null, result.Message));
        }

        [HttpPost("import")]
        [Authorize(Roles = "NURSE")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportStudents([FromForm] StudentImportRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _studentService.ImportStudentsAsync(request, cancellationToken);

            return Ok(ApiResponse<StudentImportResultDto>.Ok(result, "Import học sinh hoàn tất."));
        }

        [HttpGet("{id:int}/health-profile")]
        [Authorize(Roles = "ADMIN,NURSE,STUDENT")]
        public async Task<IActionResult> GetHealthProfile([FromRoute] int id, CancellationToken cancellationToken)
        {
            if (User.IsInRole("STUDENT"))
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim, out var currentUserId) || currentUserId != id)
                {
                    return Forbid();
                }
            }

            var data = await _studentHealthService.GetHealthProfileAsync(id, cancellationToken);

            if (data is null)
            {
                return NotFound(ApiResponse<object>.Fail("Không tìm thấy học sinh.", "id"));
            }

            return Ok(ApiResponse<StudentHealthProfileResponseDto>.Ok(data, "Lấy hồ sơ sức khỏe thành công."));
        }

        [HttpGet("{id:int}/health-profile/class-growth-comparison")]
        [Authorize(Roles = "ADMIN,NURSE,STUDENT")]
        public async Task<IActionResult> GetClassGrowthComparison(
            [FromRoute] int id,
            [FromQuery] string? metric,
            CancellationToken cancellationToken)
        {
            if (User.IsInRole("STUDENT"))
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim, out var currentUserId) || currentUserId != id)
                {
                    return Forbid();
                }
            }

            var data = await _studentHealthService.GetClassGrowthComparisonAsync(id, metric, cancellationToken);

            if (data is null)
            {
                return NotFound(ApiResponse<object>.Fail("Không tìm thấy học sinh.", "id"));
            }

            return Ok(ApiResponse<ClassGrowthComparisonResponseDto>.Ok(data, "Lấy dữ liệu so sánh trong lớp thành công."));
        }

        [HttpPatch("{id:int}/health-profile")]
        [Authorize(Roles = "NURSE")]
        public async Task<IActionResult> UpdateHealthProfile(
            [FromRoute] int id,
            [FromBody] UpdateStudentHealthProfileRequestDto request,
            CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var nurseUserId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ."));
            }

            var (success, message, field, data) = await _studentHealthService.UpdateHealthProfileAsync(
                nurseUserId,
                id,
                request,
                cancellationToken);

            if (!success)
            {
                if (field == "id")
                {
                    return NotFound(ApiResponse<object>.Fail(message, field));
                }

                return BadRequest(ApiResponse<object>.Fail(message, field));
            }

            return Ok(ApiResponse<StudentHealthProfileResponseDto>.Ok(data, message));
        }

        [HttpGet("{id:int}/health-history")]
        [Authorize(Roles = "ADMIN,NURSE,STUDENT")]
        public async Task<IActionResult> GetHealthHistory(
            [FromRoute] int id,
            [FromQuery] StudentHealthHistoryQueryDto query,
            CancellationToken cancellationToken)
        {
            if (User.IsInRole("STUDENT"))
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim, out var currentUserId) || currentUserId != id)
                {
                    return Forbid();
                }
            }

            var result = await _studentHealthService.GetHealthHistoryAsync(id, query, cancellationToken);
            if (result is null)
            {
                return NotFound(ApiResponse<object>.Fail("Không tìm thấy học sinh.", "id"));
            }

            var (items, totalItems, totalPages, page, pageSize) = result.Value;
            return Ok(new ApiResponse<IReadOnlyList<StudentHealthHistoryItemDto>>
            {
                Success = true,
                Message = "Lấy lịch sử sức khỏe thành công.",
                Data = items,
                Meta = new
                {
                    page,
                    pageSize,
                    totalItems,
                    totalPages
                }
            });
        }

        [HttpGet("{id:int}/vaccinations")]
        [Authorize(Roles = "ADMIN,NURSE,STUDENT")]
        public async Task<IActionResult> GetVaccinations([FromRoute] int id, CancellationToken cancellationToken)
        {
            if (User.IsInRole("STUDENT"))
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim, out var currentUserId) || currentUserId != id)
                {
                    return Forbid();
                }
            }

            var data = await _vaccinationService.GetStudentVaccinationHistoryAsync(id, cancellationToken);

            if (data is null)
            {
                return NotFound(ApiResponse<object>.Fail("Không tìm thấy học sinh.", "id"));
            }

            return Ok(ApiResponse<IReadOnlyList<StudentVaccinationHistoryItemDto>>.Ok(data, "Lấy lịch sử tiêm chủng thành công."));
        }
    }
}
