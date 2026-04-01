using EduHealth.DTOs.Common;
using EduHealth.DTOs.Notifications;
using EduHealth.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduHealth.Controllers
{
    [ApiController]
    [Route("api/v1/notifications")]
    [Authorize(Roles = "NURSE")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost("recipients/preview")]
        public async Task<IActionResult> PreviewRecipients([FromBody] NotificationRecipientsPreviewRequestDto request, CancellationToken cancellationToken)
        {
            if ((request.UserIds is null || request.UserIds.Count == 0) && (!request.ClassId.HasValue || request.ClassId.Value <= 0))
            {
                return BadRequest(ApiResponse<object>.Fail("Bạn phải chọn lớp hoặc danh sách người nhận.", "target"));
            }

            var result = await _notificationService.PreviewRecipientsAsync(request, cancellationToken);

            return Ok(ApiResponse<NotificationRecipientsPreviewResponseDto>.Ok(result, "Lấy danh sách người nhận thành công."));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNotificationRequestDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(ApiResponse<object>.Fail("Tiêu đề không được rỗng.", "title"));
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(ApiResponse<object>.Fail("Nội dung không được rỗng.", "content"));
            }

            if (string.IsNullOrWhiteSpace(request.Type))
            {
                return BadRequest(ApiResponse<object>.Fail("Loại thông báo không hợp lệ.", "type"));
            }

            if ((request.RecipientUserIds is null || request.RecipientUserIds.Count == 0) && (!request.ClassId.HasValue || request.ClassId.Value <= 0))
            {
                return BadRequest(ApiResponse<object>.Fail("Bạn phải chọn lớp hoặc danh sách người nhận.", "recipientUserIds"));
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ."));
            }

            var result = await _notificationService.CreateAsync(userId, request, cancellationToken);

            return Ok(ApiResponse<CreateNotificationResponseDto>.Ok(result, "Gửi thông báo thành công."));
        }

        [HttpPatch("{notificationId:int}/read")]
        [Authorize]
        public async Task<IActionResult> MarkRead([FromRoute] int notificationId, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ."));
            }

            var ok = await _notificationService.MarkReadAsync(userId, notificationId, cancellationToken);

            if (!ok)
            {
                return NotFound(ApiResponse<object>.Fail("Không tìm thấy thông báo."));
            }

            return Ok(ApiResponse<object>.Ok(null, "Đánh dấu đã đọc thành công."));
        }
    }
}
