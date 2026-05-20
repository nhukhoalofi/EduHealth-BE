using EduHealth.DTOs.Common;
using EduHealth.DTOs.Messaging;
using EduHealth.Hubs;
using EduHealth.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EduHealth.Controllers
{
    [ApiController]
    [Route("api/v1/messaging")]
    [Authorize(Roles = "NURSE,STUDENT")]
    public class MessagingController : ControllerBase
    {
        private readonly IMessagingService _messagingService;
        private readonly IHubContext<ChatHub> _chatHubContext;

        public MessagingController(IMessagingService messagingService, IHubContext<ChatHub> chatHubContext)
        {
            _messagingService = messagingService;
            _chatHubContext = chatHubContext;
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations([FromQuery] ConversationListQueryDto query, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out var userId, out var role))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ."));
            }

            var (success, error, data) = await _messagingService.GetConversationsAsync(userId, role, query, cancellationToken);
            if (!success)
            {
                return MapMessagingError(error);
            }

            return Ok(ApiResponse<PagedResultDto<ConversationListItemDto>>.Ok(data, "Lấy danh sách cuộc trò chuyện thành công."));
        }

        [HttpPost("conversations")]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequestDto request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out var userId, out var role))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ."));
            }

            var (success, error, data, _) = await _messagingService.GetOrCreateConversationAsync(userId, role, request, cancellationToken);
            if (!success)
            {
                return MapMessagingError(error);
            }

            return Ok(ApiResponse<ConversationDetailDto>.Ok(data, "Lấy cuộc trò chuyện thành công."));
        }

        [HttpGet("conversations/{conversationId:int}")]
        public async Task<IActionResult> GetConversationDetail([FromRoute] int conversationId, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out var userId, out _))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ."));
            }

            var (success, error, data) = await _messagingService.GetConversationDetailAsync(userId, conversationId, cancellationToken);
            if (!success)
            {
                return MapMessagingError(error);
            }

            return Ok(ApiResponse<ConversationDetailDto>.Ok(data, "Lấy chi tiết cuộc trò chuyện thành công."));
        }

        [HttpGet("conversations/{conversationId:int}/messages")]
        public async Task<IActionResult> GetMessages([FromRoute] int conversationId, [FromQuery] MessageListQueryDto query, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out var userId, out _))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ."));
            }

            var (success, error, data) = await _messagingService.GetMessagesAsync(userId, conversationId, query, cancellationToken);
            if (!success)
            {
                return MapMessagingError(error);
            }

            return Ok(ApiResponse<PagedResultDto<MessageItemDto>>.Ok(data, "Lấy tin nhắn thành công."));
        }

        [HttpPatch("conversations/{conversationId:int}/read")]
        public async Task<IActionResult> MarkRead([FromRoute] int conversationId, [FromBody] MarkConversationReadRequestDto request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out var userId, out _))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ."));
            }

            var (success, error, data) = await _messagingService.MarkConversationReadAsync(userId, conversationId, request, cancellationToken);
            if (!success)
            {
                return MapMessagingError(error);
            }

            return Ok(ApiResponse<ConversationReadDto>.Ok(data, "Đánh dấu đã đọc thành công."));
        }

        [HttpPost("conversations/{conversationId:int}/messages")]
        [Consumes("application/json")]
        public async Task<IActionResult> SendMessage([FromRoute] int conversationId, [FromBody] SendMessageRequestDto request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out var userId, out _))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ."));
            }

            var (success, error, data, updates) = await _messagingService.SendMessageAsync(userId, conversationId, request, cancellationToken);
            if (!success)
            {
                return MapMessagingError(error);
            }

            if (data is not null)
            {
                if (updates.Count > 0)
                {
                    await _chatHubContext.Clients.Users(updates.Keys.Select(x => x.ToString()))
                        .SendAsync("MessageCreated", data, cancellationToken);
                }
                else
                {
                    await _chatHubContext.Clients.Group(ChatHub.BuildConversationGroup(conversationId))
                        .SendAsync("MessageCreated", data, cancellationToken);
                }

                foreach (var update in updates)
                {
                    await _chatHubContext.Clients.User(update.Key.ToString())
                        .SendAsync("ConversationUpdated", update.Value, cancellationToken);
                }
            }

            return Ok(ApiResponse<MessageItemDto>.Ok(data, "Gửi tin nhắn thành công."));
        }

        [HttpPost("conversations/{conversationId:int}/messages")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SendMessageWithAttachments([FromRoute] int conversationId, [FromForm] SendMessageRequestDto request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out var userId, out _))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token khÃ´ng há»£p lá»‡."));
            }

            var (success, error, data, updates) = await _messagingService.SendMessageAsync(userId, conversationId, request, cancellationToken);
            if (!success)
            {
                return MapMessagingError(error);
            }

            if (data is not null)
            {
                if (updates.Count > 0)
                {
                    await _chatHubContext.Clients.Users(updates.Keys.Select(x => x.ToString()))
                        .SendAsync("MessageCreated", data, cancellationToken);
                }
                else
                {
                    await _chatHubContext.Clients.Group(ChatHub.BuildConversationGroup(conversationId))
                        .SendAsync("MessageCreated", data, cancellationToken);
                }

                foreach (var update in updates)
                {
                    await _chatHubContext.Clients.User(update.Key.ToString())
                        .SendAsync("ConversationUpdated", update.Value, cancellationToken);
                }
            }

            return Ok(ApiResponse<MessageItemDto>.Ok(data, "Gá»­i tin nháº¯n thÃ nh cÃ´ng."));
        }

        [HttpGet("contacts/students")]
        [Authorize(Roles = "NURSE")]
        public async Task<IActionResult> GetStudentContacts([FromQuery] ConversationListQueryDto query, [FromQuery] string? className, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out var userId, out _))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ."));
            }

            var (success, error, data) = await _messagingService.GetStudentContactsAsync(userId, query, className, cancellationToken);
            if (!success)
            {
                return MapMessagingError(error);
            }

            return Ok(ApiResponse<PagedResultDto<MessagingContactDto>>.Ok(data, "Lấy danh sách học sinh thành công."));
        }

        [HttpGet("contacts/nurses")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> GetNurseContacts([FromQuery] string? keyword, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out var userId, out _))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ."));
            }

            var (success, error, data) = await _messagingService.GetNurseContactsAsync(userId, keyword, cancellationToken);
            if (!success)
            {
                return MapMessagingError(error);
            }

            return Ok(ApiResponse<IReadOnlyList<MessagingContactDto>>.Ok(data, "Lấy danh sách y tá thành công."));
        }

        private bool TryGetCurrentUser(out int userId, out string role)
        {
            userId = 0;
            role = string.Empty;

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

            return !string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out userId);
        }

        private IActionResult MapMessagingError(MessagingErrorDto? error)
        {
            if (error is null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<object>.Fail("Đã xảy ra lỗi."));
            }

            return error.Code switch
            {
                "CONVERSATION_NOT_FOUND" => NotFound(ApiResponse<object>.Fail(error.Message)),
                "CONVERSATION_ACCESS_DENIED" => StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(error.Message)),
                "MESSAGE_ACCESS_DENIED" => StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(error.Message)),
                "ROLE_NOT_ALLOWED" => StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(error.Message)),
                "CREATE_CONVERSATION_DENIED" => BadRequest(ApiResponse<object>.Fail(error.Message)),
                "MESSAGE_CONTENT_REQUIRED" => BadRequest(ApiResponse<object>.Fail(error.Message, "content")),
                "MESSAGE_TOO_LONG" => BadRequest(ApiResponse<object>.Fail(error.Message, "content")),
                "INVALID_MESSAGE_TYPE" => BadRequest(ApiResponse<object>.Fail(error.Message, "messageType")),
                "INVALID_ATTACHMENT" => BadRequest(ApiResponse<object>.Fail(error.Message, "files")),
                "ATTACHMENT_UPLOAD_FAILED" => StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<object>.Fail(error.Message, "files")),
                "INVALID_LAST_READ_MESSAGE" => BadRequest(ApiResponse<object>.Fail(error.Message, "lastReadMessageId")),
                _ => StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<object>.Fail(error.Message))
            };
        }
    }
}
