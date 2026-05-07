using EduHealth.Data.Entities;

namespace EduHealth.Repositories.Interfaces
{
    public interface IMessagingRepository
    {
        Task<Conversation?> GetConversationByIdAsync(int conversationId, CancellationToken cancellationToken = default);
        Task<Conversation?> GetConversationWithParticipantsAsync(int conversationId, CancellationToken cancellationToken = default);
        Task<Conversation?> GetConversationForUpdateAsync(int conversationId, CancellationToken cancellationToken = default);
        Task<Conversation?> GetDirectConversationAsync(int userId, int otherUserId, CancellationToken cancellationToken = default);

        Task<ConversationParticipant?> GetParticipantAsync(int conversationId, int userId, CancellationToken cancellationToken = default);
        Task<ConversationParticipant?> GetParticipantForUpdateAsync(int conversationId, int userId, CancellationToken cancellationToken = default);
        Task<List<ConversationParticipant>> GetParticipantsAsync(int conversationId, CancellationToken cancellationToken = default);
        Task<bool> IsParticipantAsync(int conversationId, int userId, CancellationToken cancellationToken = default);

        Task<(List<Conversation> Items, int Total)> GetUserConversationsAsync(int userId, string? keyword, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<(List<ChatMessage> Items, int Total)> GetMessagesAsync(int conversationId, int page, int pageSize, int? beforeMessageId, CancellationToken cancellationToken = default);

        Task<ChatMessage?> GetLatestMessageAsync(int conversationId, CancellationToken cancellationToken = default);
        Task<ChatMessage?> GetMessageByIdAsync(int messageId, CancellationToken cancellationToken = default);

        Task<int> GetUnreadCountAsync(int conversationId, int userId, int? lastReadMessageId, CancellationToken cancellationToken = default);
        Task<Dictionary<int, int>> GetUnreadCountsAsync(IReadOnlyList<int> conversationIds, int userId, CancellationToken cancellationToken = default);

        Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<Student?> GetStudentByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<(List<Student> Items, int Total)> GetStudentContactsAsync(string? keyword, string? className, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<List<User>> GetNurseContactsAsync(string? keyword, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<(int OtherUserId, int ConversationId, DateTime? LastMessageAt)>> GetDirectConversationLookupsAsync(int userId, IReadOnlyList<int> otherUserIds, CancellationToken cancellationToken = default);

        Task AddConversationAsync(Conversation conversation, CancellationToken cancellationToken = default);
        Task AddParticipantsAsync(IReadOnlyList<ConversationParticipant> participants, CancellationToken cancellationToken = default);
        Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
        Task AddAttachmentsAsync(IReadOnlyList<ChatMessageAttachment> attachments, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
