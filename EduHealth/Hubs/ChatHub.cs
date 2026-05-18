using EduHealth.DTOs.Messaging;
using EduHealth.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EduHealth.Hubs
{
    [Authorize(Roles = "NURSE,STUDENT")]
    public class ChatHub : Hub
    {
        private readonly IMessagingService _messagingService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IMessagingService messagingService, ILogger<ChatHub> logger)
        {
            _messagingService = messagingService;
            _logger = logger;
        }

        public async Task JoinConversation(ConversationRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out var userId))
            {
                _logger.LogWarning("JoinConversation: unauthorized connection {ConnectionId}", Context.ConnectionId);
                await SendErrorAsync(BuildError("UNAUTHORIZED", "Token không hợp lệ."), cancellationToken);
                return;
            }

            var (success, error) = await _messagingService.EnsureParticipantAsync(request.ConversationId, userId, cancellationToken);
            if (!success)
            {
                _logger.LogWarning("JoinConversation denied: user {UserId} conversation {ConversationId}", userId, request.ConversationId);
                await SendErrorAsync(error ?? BuildError("CONVERSATION_ACCESS_DENIED", "Bạn không có quyền truy cập cuộc trò chuyện.", request.ConversationId), cancellationToken);
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, BuildConversationGroup(request.ConversationId), cancellationToken);
            _logger.LogInformation("JoinConversation ok: user {UserId} conversation {ConversationId} connection {ConnectionId}", userId, request.ConversationId, Context.ConnectionId);
            await Clients.Caller.SendAsync("JoinedConversation", new
            {
                conversationId = request.ConversationId,
                joinedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        public async Task LeaveConversation(ConversationRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out var userId))
            {
                await SendErrorAsync(BuildError("UNAUTHORIZED", "Token không hợp lệ."), cancellationToken);
                return;
            }

            var (success, error) = await _messagingService.EnsureParticipantAsync(request.ConversationId, userId, cancellationToken);
            if (!success)
            {
                await SendErrorAsync(error ?? BuildError("CONVERSATION_ACCESS_DENIED", "Bạn không có quyền truy cập cuộc trò chuyện.", request.ConversationId), cancellationToken);
                return;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, BuildConversationGroup(request.ConversationId), cancellationToken);
            await Clients.Caller.SendAsync("LeftConversation", new
            {
                conversationId = request.ConversationId,
                leftAt = DateTime.UtcNow
            }, cancellationToken);
        }

        public async Task SendMessage(SendMessageHubRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out var userId))
            {
                _logger.LogWarning("SendMessage: unauthorized connection {ConnectionId}", Context.ConnectionId);
                await SendErrorAsync(BuildError("UNAUTHORIZED", "Token không hợp lệ.", request.ConversationId, request.ClientMessageId), cancellationToken);
                return;
            }

            var sendRequest = new SendMessageRequestDto
            {
                Content = request.Content,
                MessageType = request.MessageType,
                ClientMessageId = request.ClientMessageId,
                AttachmentIds = request.AttachmentIds
            };

            var (success, error, message, updates) = await _messagingService.SendMessageAsync(userId, request.ConversationId, sendRequest, cancellationToken);
            if (!success || message is null)
            {
                _logger.LogWarning("SendMessage failed: user {UserId} conversation {ConversationId}", userId, request.ConversationId);
                await SendErrorAsync(error ?? BuildError("SEND_MESSAGE_FAILED", "Gửi tin nhắn thất bại.", request.ConversationId, request.ClientMessageId), cancellationToken);
                return;
            }

            if (updates.Count > 0)
            {
                await Clients.Users(updates.Keys.Select(x => x.ToString()))
                    .SendAsync("MessageCreated", message, cancellationToken);
            }
            else
            {
                await Clients.Group(BuildConversationGroup(request.ConversationId))
                    .SendAsync("MessageCreated", message, cancellationToken);
            }

            _logger.LogInformation("MessageCreated broadcast: conversation {ConversationId} users {UserCount}", request.ConversationId, updates.Count);

            foreach (var update in updates)
            {
                await Clients.User(update.Key.ToString())
                    .SendAsync("ConversationUpdated", update.Value, cancellationToken);
            }
        }

        public async Task Typing(TypingRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out var userId))
            {
                await SendErrorAsync(BuildError("UNAUTHORIZED", "Token không hợp lệ.", request.ConversationId), cancellationToken);
                return;
            }

            var (accessOk, error) = await _messagingService.EnsureParticipantAsync(request.ConversationId, userId, cancellationToken);
            if (!accessOk)
            {
                await SendErrorAsync(error ?? BuildError("CONVERSATION_ACCESS_DENIED", "Bạn không có quyền truy cập cuộc trò chuyện.", request.ConversationId), cancellationToken);
                return;
            }

            var (userOk, userError, userSummary) = await _messagingService.GetUserSummaryAsync(userId, cancellationToken);
            if (!userOk || userSummary is null)
            {
                await SendErrorAsync(userError ?? BuildError("UNAUTHORIZED", "Không tìm thấy người dùng.", request.ConversationId), cancellationToken);
                return;
            }

            var payload = new TypingEventDto
            {
                ConversationId = request.ConversationId,
                UserId = userSummary.UserId,
                FullName = userSummary.FullName,
                Role = userSummary.Role,
                IsTyping = request.IsTyping,
                SentAt = DateTime.UtcNow
            };

            await Clients.OthersInGroup(BuildConversationGroup(request.ConversationId))
                .SendAsync("TypingChanged", payload, cancellationToken);
        }

        public async Task MarkConversationRead(MarkReadRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out var userId))
            {
                await SendErrorAsync(BuildError("UNAUTHORIZED", "Token không hợp lệ.", request.ConversationId), cancellationToken);
                return;
            }

            var markRequest = new MarkConversationReadRequestDto
            {
                LastReadMessageId = request.LastReadMessageId
            };

            var (success, error, data) = await _messagingService.MarkConversationReadAsync(userId, request.ConversationId, markRequest, cancellationToken);
            if (!success || data is null)
            {
                await SendErrorAsync(error ?? BuildError("INVALID_LAST_READ_MESSAGE", "Tin nhắn không hợp lệ.", request.ConversationId), cancellationToken);
                return;
            }

            await Clients.Group(BuildConversationGroup(request.ConversationId))
                .SendAsync("ConversationRead", data, cancellationToken);
        }

        private bool TryGetCurrentUser(out int userId)
        {
            userId = 0;
            var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out userId);
        }

        public static string BuildConversationGroup(int conversationId) => $"conversation:{conversationId}";

        private Task SendErrorAsync(MessagingErrorDto error, CancellationToken cancellationToken)
        {
            return Clients.Caller.SendAsync("MessagingError", error, cancellationToken);
        }

        private static MessagingErrorDto BuildError(string code, string message, int? conversationId = null, string? clientMessageId = null)
        {
            return new MessagingErrorDto
            {
                Code = code,
                Message = message,
                ConversationId = conversationId,
                ClientMessageId = clientMessageId
            };
        }

        public class ConversationRequest
        {
            public int ConversationId { get; set; }
        }

        public class TypingRequest
        {
            public int ConversationId { get; set; }
            public bool IsTyping { get; set; }
        }

        public class MarkReadRequest
        {
            public int ConversationId { get; set; }
            public int? LastReadMessageId { get; set; }
        }

        public class SendMessageHubRequest
        {
            public int ConversationId { get; set; }
            public string? Content { get; set; }
            public string? MessageType { get; set; }
            public string? ClientMessageId { get; set; }
            public IReadOnlyList<int>? AttachmentIds { get; set; }
        }
    }
}
