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
        private const long MaxImageSizeBytes = 5 * 1024 * 1024;
        private const string NotificationImageFolder = "eduhealth/notifications";

        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        private readonly INotificationService _notificationService;
        private readonly ISseNotificationService _sseService;
        private readonly ICloudinaryService _cloudinaryService;

        public NotificationsController(
            INotificationService notificationService,
            ISseNotificationService sseService,
            ICloudinaryService cloudinaryService)
        {
            _notificationService = notificationService;
            _sseService = sseService;
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost("recipients/preview")]
        [Authorize(Roles = "ADMIN,NURSE")]
        public async Task<IActionResult> PreviewRecipients([FromBody] NotificationRecipientsPreviewRequestDto request, CancellationToken cancellationToken)
        {
            if ((request.UserIds is null || request.UserIds.Count == 0) && (!request.ClassId.HasValue || request.ClassId.Value <= 0))
            {
                return BadRequest(ApiResponse<object>.Fail("Bạn phải chọn lớp hoặc danh sách người nhận.", "target"));
            }

            var result = await _notificationService.PreviewRecipientsAsync(request, cancellationToken);

            return Ok(ApiResponse<NotificationRecipientsPreviewResponseDto>.Ok(result, "Lấy danh sách người nhận thành công."));
        }

        [HttpPost("upload-image")]
        [Authorize(Roles = "ADMIN,NURSE")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile? file, CancellationToken cancellationToken)
        {
            if (!User.IsInRole("ADMIN") && !User.IsInRole("NURSE"))
            {
                return StatusCode(403, ApiResponse<object>.Fail("Bạn không có quyền upload ảnh thông báo.", "role"));
            }

            var validation = ValidateNotificationImage(file);
            if (!validation.Success)
            {
                return BadRequest(ApiResponse<object>.Fail(validation.Message, "file"));
            }

            try
            {
                var (url, publicId) = await _cloudinaryService.UploadImageAsync(file!, NotificationImageFolder, cancellationToken);

                return Ok(ApiResponse<UploadNotificationImageResponseDto>.Ok(new UploadNotificationImageResponseDto
                {
                    Url = url,
                    PublicId = publicId
                }, "Upload ảnh thông báo thành công."));
            }
            catch
            {
                return StatusCode(500, ApiResponse<object>.Fail("Upload ảnh thông báo thất bại.", "file"));
            }
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,NURSE")]
        public async Task<IActionResult> Create([FromBody] CreateNotificationRequestDto request, CancellationToken cancellationToken)
        {
            if (!User.IsInRole("ADMIN") && !User.IsInRole("NURSE"))
            {
                return StatusCode(403, ApiResponse<object>.Fail("Bạn không có quyền tạo thông báo.", "role"));
            }

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

            var validation = await _notificationService.ValidateCreateAsync(request, cancellationToken);
            if (!validation.Success)
            {
                return BadRequest(ApiResponse<object>.Fail(validation.Message, validation.Field));
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ."));
            }

            var result = await _notificationService.CreateAsync(userId, request, cancellationToken);

            return Ok(ApiResponse<CreateNotificationResponseDto>.Ok(result, "Gửi thông báo thành công."));
        }

        private static (bool Success, string Message) ValidateNotificationImage(IFormFile? file)
        {
            if (file is null || file.Length == 0)
            {
                return (false, "Vui lòng chọn file ảnh thông báo.");
            }

            if (file.Length > MaxImageSizeBytes)
            {
                return (false, "Dung lượng ảnh thông báo không được vượt quá 5MB.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
            {
                return (false, "Ảnh thông báo chỉ hỗ trợ jpg, jpeg, png, webp.");
            }

            if (string.IsNullOrWhiteSpace(file.ContentType) || !AllowedImageContentTypes.Contains(file.ContentType))
            {
                return (false, "Ảnh thông báo chỉ hỗ trợ jpg, jpeg, png, webp.");
            }

            return (true, string.Empty);
        }

        [HttpGet("sent")]
        [Authorize(Roles = "ADMIN,NURSE")]
        public async Task<IActionResult> GetSentNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            if (!User.IsInRole("ADMIN") && !User.IsInRole("NURSE"))
            {
                return StatusCode(403, ApiResponse<object>.Fail("Bạn không có quyền xem danh sách thông báo đã gửi.", "role"));
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ."));
            }

            var result = await _notificationService.GetSentNotificationsAsync(userId, page, pageSize, cancellationToken);

            return Ok(ApiResponse<SentNotificationsResponseDto>.Ok(result, "Lấy danh sách thông báo đã gửi thành công."));
        }

        [HttpPatch("{notificationId:int}/read")]
        [Authorize(Roles = "ADMIN,NURSE,STUDENT")]
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

        [HttpGet]
        [Authorize(Roles = "ADMIN,NURSE,STUDENT")]
        public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ."));
            }

            var result = await _notificationService.GetNotificationsAsync(userId, page, pageSize, cancellationToken);

            return Ok(ApiResponse<GetNotificationsResponseDto>.Ok(result, "Lấy danh sách thông báo thành công."));
        }

        [HttpPatch("read-all")]
        [Authorize(Roles = "ADMIN,NURSE,STUDENT")]
        public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ."));
            }

            var count = await _notificationService.MarkAllReadAsync(userId, cancellationToken);

            return Ok(ApiResponse<object>.Ok(new { markedCount = count }, "Đánh dấu tất cả đã đọc thành công."));
        }

        [HttpGet("stream")]
        [Authorize(Roles = "ADMIN,NURSE,STUDENT")]
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
