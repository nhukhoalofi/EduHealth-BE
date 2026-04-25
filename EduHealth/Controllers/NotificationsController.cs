using EduHealth.DTOs.Common;
using EduHealth.DTOs.Notifications;
using EduHealth.Services.Interfaces;
using EduHealth.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduHealth.Controllers
{
    [ApiController]
    [Route("api/v1/notifications")]
    [Authorize(Roles = "ADMIN,NURSE,STUDENT")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ISseNotificationService _sseService;

        public NotificationsController(INotificationService notificationService, ISseNotificationService sseService)
        {
            _notificationService = notificationService;
            _sseService = sseService;
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

        [HttpGet("stream")]
        [Authorize]
        public async Task<IActionResult> Stream(CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ."));
            }

            // Set response headers for SSE
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("Access-Control-Allow-Origin", "*");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, HttpContext.RequestAborted);

            try
            {
                // Register connection using wrapper
                var sseService = _sseService as SseNotificationService;
                var streamWriter = new StreamWriter(Response.Body, System.Text.Encoding.UTF8, leaveOpen: true);
                await sseService?.RegisterConnectionAsync(userId, streamWriter, cts.Token)!;

                // Send initial message
                await Response.WriteAsync(": SSE stream connected\n\n", cts.Token);
                await Response.Body.FlushAsync(cts.Token);

                // Keep connection alive with periodic heartbeat
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Send heartbeat every 30 seconds
                        await Task.Delay(30000, cts.Token);
                        await Response.WriteAsync(": heartbeat\n\n", cts.Token);
                        await Response.Body.FlushAsync(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            finally
            {
                // Clean up
                await _sseService.RemoveClientAsync(userId);
                cts.Cancel();
            }

            return new EmptyResult();
        }
    }
}
