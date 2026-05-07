using EduHealth.Data;
using EduHealth.Data.Entities;
using EduHealth.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Repositories.Implementations
{
    public class MessagingRepository : IMessagingRepository
    {
        private readonly AppDbContext _context;

        public MessagingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Conversation?> GetConversationByIdAsync(int conversationId, CancellationToken cancellationToken = default)
        {
            return await _context.Conversations
                .AsNoTracking()
                .Include(x => x.Student)
                .ThenInclude(x => x!.Class)
                .Include(x => x.Student)
                .ThenInclude(x => x!.User)
                .Include(x => x.Participants)
                .ThenInclude(x => x.User)
                .Include(x => x.LastMessage)
                .ThenInclude(x => x!.Sender)
                .FirstOrDefaultAsync(x => x.ConversationId == conversationId, cancellationToken);
        }

        public async Task<Conversation?> GetConversationWithParticipantsAsync(int conversationId, CancellationToken cancellationToken = default)
        {
            return await _context.Conversations
                .Include(x => x.Student)
                .ThenInclude(x => x!.Class)
                .Include(x => x.Student)
                .ThenInclude(x => x!.User)
                .Include(x => x.Participants)
                .ThenInclude(x => x.User)
                .Include(x => x.LastMessage)
                .ThenInclude(x => x!.Sender)
                .FirstOrDefaultAsync(x => x.ConversationId == conversationId, cancellationToken);
        }

        public async Task<Conversation?> GetConversationForUpdateAsync(int conversationId, CancellationToken cancellationToken = default)
        {
            return await _context.Conversations
                .Include(x => x.Participants)
                .FirstOrDefaultAsync(x => x.ConversationId == conversationId, cancellationToken);
        }

        public async Task<Conversation?> GetDirectConversationAsync(int userId, int otherUserId, CancellationToken cancellationToken = default)
        {
            return await _context.Conversations
                .Include(x => x.Student)
                .ThenInclude(x => x!.Class)
                .Include(x => x.Student)
                .ThenInclude(x => x!.User)
                .Include(x => x.Participants)
                .ThenInclude(x => x.User)
                .Include(x => x.LastMessage)
                .ThenInclude(x => x!.Sender)
                .Where(x => x.ConversationType == "DIRECT")
                .Where(x => x.Participants.Count == 2)
                .FirstOrDefaultAsync(x =>
                    x.Participants.Any(p => p.UserId == userId) &&
                    x.Participants.Any(p => p.UserId == otherUserId),
                    cancellationToken);
        }

        public async Task<ConversationParticipant?> GetParticipantAsync(int conversationId, int userId, CancellationToken cancellationToken = default)
        {
            return await _context.ConversationParticipants
                .AsNoTracking()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.ConversationId == conversationId && x.UserId == userId, cancellationToken);
        }

        public async Task<ConversationParticipant?> GetParticipantForUpdateAsync(int conversationId, int userId, CancellationToken cancellationToken = default)
        {
            return await _context.ConversationParticipants
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.ConversationId == conversationId && x.UserId == userId, cancellationToken);
        }

        public async Task<List<ConversationParticipant>> GetParticipantsAsync(int conversationId, CancellationToken cancellationToken = default)
        {
            return await _context.ConversationParticipants
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x => x.ConversationId == conversationId)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsParticipantAsync(int conversationId, int userId, CancellationToken cancellationToken = default)
        {
            return await _context.ConversationParticipants
                .AsNoTracking()
                .AnyAsync(x => x.ConversationId == conversationId && x.UserId == userId, cancellationToken);
        }

        public async Task<(List<Conversation> Items, int Total)> GetUserConversationsAsync(int userId, string? keyword, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.Conversations
                .AsNoTracking()
                .Include(x => x.Student)
                .ThenInclude(x => x!.Class)
                .Include(x => x.Student)
                .ThenInclude(x => x!.User)
                .Include(x => x.Participants)
                .ThenInclude(x => x.User)
                .Include(x => x.LastMessage)
                .ThenInclude(x => x!.Sender)
                .Where(x => x.Participants.Any(p => p.UserId == userId))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var term = keyword.Trim();
                query = query.Where(x =>
                    (x.Student != null && x.Student.FullName.Contains(term)) ||
                    x.Participants.Any(p => p.User.FullName.Contains(term)) ||
                    (x.Title != null && x.Title.Contains(term)));
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task<(List<ChatMessage> Items, int Total)> GetMessagesAsync(int conversationId, int page, int pageSize, int? beforeMessageId, CancellationToken cancellationToken = default)
        {
            var query = _context.ChatMessages
                .AsNoTracking()
                .Include(x => x.Sender)
                .Include(x => x.Attachments)
                .Where(x => x.ConversationId == conversationId)
                .AsQueryable();

            if (beforeMessageId.HasValue && beforeMessageId.Value > 0)
            {
                query = query.Where(x => x.MessageId < beforeMessageId.Value);
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.MessageId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task<ChatMessage?> GetLatestMessageAsync(int conversationId, CancellationToken cancellationToken = default)
        {
            return await _context.ChatMessages
                .AsNoTracking()
                .Where(x => x.ConversationId == conversationId)
                .OrderByDescending(x => x.MessageId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<ChatMessage?> GetMessageByIdAsync(int messageId, CancellationToken cancellationToken = default)
        {
            return await _context.ChatMessages
                .AsNoTracking()
                .Include(x => x.Sender)
                .Include(x => x.Attachments)
                .FirstOrDefaultAsync(x => x.MessageId == messageId, cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(int conversationId, int userId, int? lastReadMessageId, CancellationToken cancellationToken = default)
        {
            var lastReadId = lastReadMessageId ?? 0;

            return await _context.ChatMessages
                .AsNoTracking()
                .Where(x => x.ConversationId == conversationId && x.MessageId > lastReadId && x.SenderUserId != userId)
                .CountAsync(cancellationToken);
        }

        public async Task<Dictionary<int, int>> GetUnreadCountsAsync(IReadOnlyList<int> conversationIds, int userId, CancellationToken cancellationToken = default)
        {
            if (conversationIds.Count == 0)
            {
                return new Dictionary<int, int>();
            }

            var query = from message in _context.ChatMessages.AsNoTracking()
                        join participant in _context.ConversationParticipants.AsNoTracking()
                            on message.ConversationId equals participant.ConversationId
                        where conversationIds.Contains(message.ConversationId)
                              && participant.UserId == userId
                              && message.SenderUserId != userId
                              && message.MessageId > (participant.LastReadMessageId ?? 0)
                        group message by message.ConversationId into g
                        select new { ConversationId = g.Key, Count = g.Count() };

            var items = await query.ToListAsync(cancellationToken);

            return items.ToDictionary(x => x.ConversationId, x => x.Count);
        }

        public async Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        }

        public async Task<Student?> GetStudentByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.Students
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Class)
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        }

        public async Task<(List<Student> Items, int Total)> GetStudentContactsAsync(string? keyword, string? className, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.Students
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Class)
                .Where(x => x.User.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var term = keyword.Trim();
                query = query.Where(x => x.FullName.Contains(term) || x.User.Phone.Contains(term) || x.User.Email.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(className))
            {
                var term = className.Trim();
                query = query.Where(x => x.Class.ClassName.Contains(term));
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(x => x.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task<List<User>> GetNurseContactsAsync(string? keyword, CancellationToken cancellationToken = default)
        {
            var query = _context.Users
                .AsNoTracking()
                .Where(x => x.Role == "NURSE" && x.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var term = keyword.Trim();
                query = query.Where(x => x.FullName.Contains(term) || x.Email.Contains(term) || x.Phone.Contains(term));
            }

            return await query
                .OrderBy(x => x.FullName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<(int OtherUserId, int ConversationId, DateTime? LastMessageAt)>> GetDirectConversationLookupsAsync(
            int userId,
            IReadOnlyList<int> otherUserIds,
            CancellationToken cancellationToken = default)
        {
            if (otherUserIds.Count == 0)
            {
                return Array.Empty<(int OtherUserId, int ConversationId, DateTime? LastMessageAt)>();
            }

            var query = from self in _context.ConversationParticipants.AsNoTracking()
                        join other in _context.ConversationParticipants.AsNoTracking()
                            on self.ConversationId equals other.ConversationId
                        join conversation in _context.Conversations.AsNoTracking()
                            on self.ConversationId equals conversation.ConversationId
                        join lastMessage in _context.ChatMessages.AsNoTracking()
                            on conversation.LastMessageId equals lastMessage.MessageId into messageGroup
                        from message in messageGroup.DefaultIfEmpty()
                        where self.UserId == userId
                              && otherUserIds.Contains(other.UserId)
                              && conversation.ConversationType == "DIRECT"
                        select new
                        {
                            OtherUserId = other.UserId,
                            ConversationId = conversation.ConversationId,
                            LastMessageAt = message != null ? message.SentAt : (DateTime?)null
                        };

            var items = await query.ToListAsync(cancellationToken);

            return items
                .GroupBy(x => x.OtherUserId)
                .Select(x => (x.Key, x.First().ConversationId, x.First().LastMessageAt))
                .ToList();
        }

        public async Task AddConversationAsync(Conversation conversation, CancellationToken cancellationToken = default)
        {
            await _context.Conversations.AddAsync(conversation, cancellationToken);
        }

        public async Task AddParticipantsAsync(IReadOnlyList<ConversationParticipant> participants, CancellationToken cancellationToken = default)
        {
            await _context.ConversationParticipants.AddRangeAsync(participants, cancellationToken);
        }

        public async Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
        {
            await _context.ChatMessages.AddAsync(message, cancellationToken);
        }

        public async Task AddAttachmentsAsync(IReadOnlyList<ChatMessageAttachment> attachments, CancellationToken cancellationToken = default)
        {
            await _context.ChatMessageAttachments.AddRangeAsync(attachments, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
