using EduHealth.DTOs.Common;
using EduHealth.DTOs.Medicines;
using EduHealth.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduHealth.Controllers
{
    [ApiController]
    [Route("api/v1/medicines")]
    [Authorize]
    public class MedicinesController : ControllerBase
    {
        private readonly IMedicineService _medicineService;

        public MedicinesController(IMedicineService medicineService)
        {
            _medicineService = medicineService;
        }

        [HttpGet]
        [Authorize(Roles = "NURSE,ADMIN")]
        public async Task<IActionResult> GetPaged([FromQuery] MedicineListQueryDto query, CancellationToken cancellationToken)
        {
            var (items, totalItems, totalPages, page, pageSize) = await _medicineService.GetPagedAsync(query, cancellationToken);

            return Ok(new ApiResponseV2<IReadOnlyList<MedicineListItemDto>>
            {
                Success = true,
                Message = "Lấy danh sách thuốc thành công.",
                Data = items,
                Meta = new { page, pageSize, totalItems, totalPages },
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpPost]
        [Authorize(Roles = "NURSE")]
        public async Task<IActionResult> Create([FromBody] CreateMedicineRequestDto request, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _ = int.TryParse(userIdClaim, out var userId);
            var (success, statusCode, message, errors, data) = await _medicineService.CreateAsync(userId, request, cancellationToken);

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

            return StatusCode(statusCode ?? 201, new ApiResponseV2<MedicineDetailDto>
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
            var (found, data) = await _medicineService.GetDetailAsync(id, cancellationToken);

            if (!found)
            {
                return NotFound(new ApiErrorResponseV2
                {
                    Success = false,
                    Message = "Không tìm thấy thuốc.",
                    Errors = new[] { new ApiErrorItemDto { Field = "id", Code = "MEDICINE_NOT_FOUND", Message = "Không tồn tại thuốc với id đã cung cấp." } },
                    Timestamp = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(new ApiResponseV2<MedicineDetailDto>
            {
                Success = true,
                Message = "Lấy chi tiết thuốc thành công.",
                Data = data,
                Meta = null,
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpPatch("{id}")]
        [Authorize(Roles = "NURSE")]
        public async Task<IActionResult> Update([FromRoute] string id, [FromBody] UpdateMedicineRequestDto request, CancellationToken cancellationToken)
        {
            var (success, message, errors, data) = await _medicineService.UpdateAsync(id, request, cancellationToken);

            if (!success)
            {
                return BadRequest(new ApiErrorResponseV2
                {
                    Success = false,
                    Message = message,
                    Errors = errors.Select(x => new ApiErrorItemDto { Field = x.Field, Code = x.Code, Message = x.Message }).ToList(),
                    Timestamp = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(new ApiResponseV2<object>
            {
                Success = true,
                Message = message,
                Data = data,
                Meta = null,
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "NURSE,ADMIN")]
        public async Task<IActionResult> UpdateStatus([FromRoute] string id, [FromBody] UpdateMedicineStatusRequestDto request, CancellationToken cancellationToken)
        {
            var (success, message, errors, data) = await _medicineService.UpdateStatusAsync(id, request, cancellationToken);

            if (!success)
            {
                return BadRequest(new ApiErrorResponseV2
                {
                    Success = false,
                    Message = message,
                    Errors = errors.Select(x => new ApiErrorItemDto { Field = x.Field, Code = x.Code, Message = x.Message }).ToList(),
                    Timestamp = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(new ApiResponseV2<object>
            {
                Success = true,
                Message = message,
                Data = data,
                Meta = null,
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpPost("{id}/stock-in")]
        [Authorize(Roles = "NURSE")]
        public async Task<IActionResult> StockIn([FromRoute] string id, [FromBody] StockInMedicineRequestDto request, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _ = int.TryParse(userIdClaim, out var userId);

            var (success, message, errors, data) = await _medicineService.StockInAsync(id, userId, request, cancellationToken);

            if (!success)
            {
                return BadRequest(new ApiErrorResponseV2
                {
                    Success = false,
                    Message = message,
                    Errors = errors.Select(x => new ApiErrorItemDto { Field = x.Field, Code = x.Code, Message = x.Message }).ToList(),
                    Timestamp = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(new ApiResponseV2<StockMovementResponseDto>
            {
                Success = true,
                Message = message,
                Data = data,
                Meta = null,
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpPost("{id}/dispose")]
        [Authorize(Roles = "NURSE")]
        public async Task<IActionResult> Dispose([FromRoute] string id, [FromBody] DisposeMedicineRequestDto request, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _ = int.TryParse(userIdClaim, out var userId);

            var (success, message, errors, data) = await _medicineService.DisposeAsync(id, userId, request, cancellationToken);

            if (!success)
            {
                return BadRequest(new ApiErrorResponseV2
                {
                    Success = false,
                    Message = message,
                    Errors = errors.Select(x => new ApiErrorItemDto { Field = x.Field, Code = x.Code, Message = x.Message }).ToList(),
                    Timestamp = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(new ApiResponseV2<StockMovementResponseDto>
            {
                Success = true,
                Message = message,
                Data = data,
                Meta = null,
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpGet("{id}/movements")]
        [Authorize(Roles = "NURSE,ADMIN")]
        public async Task<IActionResult> Movements(
            [FromRoute] string id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? type = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            CancellationToken cancellationToken = default)
        {
            var (items, totalItems, totalPages, p, ps) = await _medicineService.GetMovementsAsync(id, page, pageSize, type, fromDate, toDate, cancellationToken);

            return Ok(new ApiResponseV2<IReadOnlyList<MedicineMovementItemDto>>
            {
                Success = true,
                Message = "Lấy lịch sử biến động kho thành công.",
                Data = items,
                Meta = new { page = p, pageSize = ps, totalItems, totalPages },
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpGet("alerts")]
        [Authorize(Roles = "NURSE,ADMIN")]
        public async Task<IActionResult> Alerts([FromQuery] string type, CancellationToken cancellationToken)
        {
            var data = await _medicineService.GetAlertsAsync(type, cancellationToken);

            return Ok(new ApiResponseV2<IReadOnlyList<MedicineAlertItemDto>>
            {
                Success = true,
                Message = "Lấy danh sách cảnh báo thành công.",
                Data = data,
                Meta = null,
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
