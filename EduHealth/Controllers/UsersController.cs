using EduHealth.DTOs.Common;
using EduHealth.DTOs.Users;
using EduHealth.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduHealth.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    [Authorize(Roles = "ADMIN")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPatch("{id}/image")]
        [Authorize(Roles = "ADMIN,NURSE,STUDENT")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateImage([FromRoute] string id, [FromForm] EduHealth.DTOs.Auth.UpdateAvatarRequestDto request, CancellationToken cancellationToken)
        {
            var (success, message, errors, data) = await _userService.UpdateAvatarByCodeAsync(id, request.File, cancellationToken);

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

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] UserListQueryDto query, CancellationToken cancellationToken)
        {
            var (items, totalItems, totalPages, page, pageSize) = await _userService.GetPagedAsync(query, cancellationToken);

            return Ok(new ApiResponseV2<IReadOnlyList<UserListItemDto>>
            {
                Success = true,
                Message = "Lấy danh sách tài khoản thành công.",
                Data = items,
                Meta = new
                {
                    page,
                    pageSize,
                    totalItems,
                    totalPages
                },
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequestDto request, CancellationToken cancellationToken)
        {
            var (success, statusCode, message, errors, data) = await _userService.CreateAsync(request, cancellationToken);

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

            return StatusCode(201, new ApiResponseV2<UserDetailDto>
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
        public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken cancellationToken)
        {
            var user = await _userService.GetByCodeAsync(id, cancellationToken);

            if (user is null)
            {
                return NotFound(new ApiErrorResponseV2
                {
                    Success = false,
                    Message = "Không tìm thấy tài khoản.",
                    Errors = new[]
                    {
                        new ApiErrorItemDto
                        {
                            Field = "id",
                            Code = "USER_NOT_FOUND",
                            Message = "Không tồn tại user với id đã cung cấp."
                        }
                    },
                    Timestamp = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(new ApiResponseV2<UserDetailDto>
            {
                Success = true,
                Message = "Lấy chi tiết tài khoản thành công.",
                Data = user,
                Meta = null,
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Update([FromRoute] string id, [FromBody] UpdateUserRequestDto request, CancellationToken cancellationToken)
        {
            var (success, message, errors, data) = await _userService.UpdateAsync(id, request, cancellationToken);

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

            return Ok(new ApiResponseV2<UserDetailDto>
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
        public async Task<IActionResult> UpdateStatus([FromRoute] string id, [FromBody] UpdateUserStatusRequestDto request, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _ = int.TryParse(userIdClaim, out var currentUserId);

            var (success, message, errors, data) = await _userService.UpdateStatusAsync(id, request, currentUserId, cancellationToken);

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

        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword([FromRoute] string id, [FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
        {
            var (success, message, errors, data) = await _userService.ResetPasswordAsync(id, request, cancellationToken);

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

            return Ok(new ApiResponseV2<ResetPasswordResponseDto>
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
