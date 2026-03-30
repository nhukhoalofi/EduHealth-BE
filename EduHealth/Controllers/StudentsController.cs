using EduHealth.DTOs.Common;
using EduHealth.DTOs.Students;
using EduHealth.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHealth.Controllers
{
    [ApiController]
    [Route("api/v1/students")]
    [Authorize]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentsController(IStudentService studentService)
        {
            _studentService = studentService;
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
        [Authorize(Roles = "NURSE")]
        public async Task<IActionResult> UpdateStudent([FromRoute] int id, [FromBody] StudentUpdateRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _studentService.UpdateStudentAsync(id, request, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.Fail(result.Message, result.Field));
            }

            return Ok(ApiResponse<object>.Ok(null, result.Message));
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
    }
}