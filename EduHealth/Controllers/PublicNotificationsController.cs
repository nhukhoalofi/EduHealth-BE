using EduHealth.DTOs.Common;
using EduHealth.DTOs.Notifications;
using EduHealth.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHealth.Controllers
{
    [ApiController]
    [Route("api/v1/public/notifications")]
    [AllowAnonymous]
    public class PublicNotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public PublicNotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPublicNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 6,
            [FromQuery] string? type = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _notificationService.GetPublicNotificationsAsync(page, pageSize, type, cancellationToken);

            return Ok(ApiResponse<PublicNotificationsResponseDto>.Ok(result, "Lấy bản tin y tế thành công."));
        }
    }
}
