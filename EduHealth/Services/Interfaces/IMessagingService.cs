using EduHealth.DTOs.Messaging;

namespace EduHealth.Services.Interfaces
{
    public interface IMessagingService
    {
        Task<(bool Success, MessagingErrorDto? Error, PagedResultDto<ConversationListItemDto>? Data)> GetConversationsAsync(
            int currentUserId,
            string role,
            ConversationListQueryDto query,
            CancellationToken cancellationToken = default);

        Task<(bool Success, MessagingErrorDto? Error, ConversationDetailDto? Data, bool IsCreated)> GetOrCreateConversationAsync(
            int currentUserId,
            string role,
            CreateConversationRequestDto request,
            CancellationToken cancellationToken = default);

        Task<(bool Success, MessagingErrorDto? Error, ConversationDetailDto? Data)> GetConversationDetailAsync(
            int currentUserId,
            int conversationId,
            CancellationToken cancellationToken = default);

        Task<(bool Success, MessagingErrorDto? Error, PagedResultDto<MessageItemDto>? Data)> GetMessagesAsync(
            int currentUserId,
            int conversationId,
            MessageListQueryDto query,
            CancellationToken cancellationToken = default);

        Task<(bool Success, MessagingErrorDto? Error, ConversationReadDto? Data)> MarkConversationReadAsync(
            int currentUserId,
            int conversationId,
            MarkConversationReadRequestDto request,
            CancellationToken cancellationToken = default);

        Task<(bool Success, MessagingErrorDto? Error, MessageItemDto? Data, Dictionary<int, ConversationListItemDto> ConversationUpdates)> SendMessageAsync(
            int currentUserId,
            int conversationId,
            SendMessageRequestDto request,
            CancellationToken cancellationToken = default);

        Task<(bool Success, MessagingErrorDto? Error, PagedResultDto<MessagingContactDto>? Data)> GetStudentContactsAsync(
            int nurseUserId,
            ConversationListQueryDto query,
            string? className,
            CancellationToken cancellationToken = default);

        Task<(bool Success, MessagingErrorDto? Error, IReadOnlyList<MessagingContactDto>? Data)> GetNurseContactsAsync(
            int studentUserId,
            string? keyword,
            CancellationToken cancellationToken = default);

        Task<(bool Success, MessagingErrorDto? Error, ConversationParticipantDto? User)> GetUserSummaryAsync(
            int userId,
            CancellationToken cancellationToken = default);

        Task<(bool Success, MessagingErrorDto? Error)> EnsureParticipantAsync(
            int conversationId,
            int userId,
            CancellationToken cancellationToken = default);
    }
}
