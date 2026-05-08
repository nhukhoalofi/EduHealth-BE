using EduHealth.Data;
using EduHealth.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Controllers
{
    [ApiController]
    [Route("api/v1/classes")]
    [Authorize(Roles = "ADMIN,NURSE")]
    public class ClassesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClassesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetClasses(CancellationToken cancellationToken)
        {
            var classes = await _context.SchoolClasses
                .Select(c => new
                {
                    classId = c.ClassId,
                    className = c.ClassName
                })
                .OrderBy(c => c.className)
                .ToListAsync(cancellationToken);

            return Ok(ApiResponse<object>.Ok(classes, "Lấy danh sách lớp học thành công."));
        }
    }
}
