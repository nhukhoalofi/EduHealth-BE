using EduHealth.Data.Entities;
using EduHealth.DTOs.Messaging;
using EduHealth.Repositories.Interfaces;
using EduHealth.Services.Interfaces;

namespace EduHealth.Services.Implementations
{
    public class MessagingService : IMessagingService
    {
        private const string RoleNurse = "NURSE";
        private const string RoleStudent = "STUDENT";
        private const string MessageTypeText = "TEXT";

        private readonly IMessagingRepository _messagingRepository;

        public MessagingService(IMessagingRepository messagingRepository)
        {
            _messagingRepository = messagingRepository;
        }

        public async Task<(bool Success, MessagingErrorDto? Error, PagedResultDto<ConversationListItemDto>? Data)> GetConversationsAsync(
            int currentUserId,
            string role,
            ConversationListQueryDto query,
            CancellationToken cancellationToken = default)
        {
            if (!IsMessagingRole(role))
            {
                return (false, BuildError("ROLE_NOT_ALLOWED", "Bạn không có quyền dùng Messaging."), null);
            }

            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);

            var (items, total) = await _messagingRepository.GetUserConversationsAsync(
                currentUserId,
                query.Keyword,
                page,
                pageSize,
                cancellationToken);

            var conversationIds = items.Select(x => x.ConversationId).ToList();
            var unreadCounts = await _messagingRepository.GetUnreadCountsAsync(conversationIds, currentUserId, cancellationToken);

            var mapped = items.Select(x => MapConversationListItem(x, currentUserId, unreadCounts)).ToList();

            var result = new PagedResultDto<ConversationListItemDto>
            {
                Items = mapped,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize)
            };

            return (true, null, result);
        }

        public async Task<(bool Success, MessagingErrorDto? Error, ConversationDetailDto? Data, bool IsCreated)> GetOrCreateConversationAsync(
            int currentUserId,
            string role,
            CreateConversationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (!IsMessagingRole(role))
            {
                return (false, BuildError("ROLE_NOT_ALLOWED", "Bạn không có quyền dùng Messaging."), null, false);
            }

            if (request.ParticipantUserId <= 0)
            {
                return (false, BuildError("CREATE_CONVERSATION_DENIED", "Người tham gia không hợp lệ."), null, false);
            }

            var participantUser = await _messagingRepository.GetUserByIdAsync(request.ParticipantUserId, cancellationToken);
            if (participantUser is null)
            {
                return (false, BuildError("CREATE_CONVERSATION_DENIED", "Không tìm thấy người tham gia."), null, false);
            }

            if (!IsValidPair(role, participantUser.Role))
            {
                return (false, BuildError("CREATE_CONVERSATION_DENIED", "Chỉ cho phép chat giữa NURSE và STUDENT."), null, false);
            }

            var expectedStudentId = role == RoleStudent ? currentUserId : request.ParticipantUserId;
            if (!request.StudentId.HasValue || request.StudentId.Value <= 0)
            {
                request.StudentId = expectedStudentId;
            }
            else if (request.StudentId.Value != expectedStudentId)
            {
                return (false, BuildError("CREATE_CONVERSATION_DENIED", "studentId không hợp lệ."), null, false);
            }

            var resolvedStudentId = request.StudentId.Value;

            var student = await _messagingRepository.GetStudentByUserIdAsync(resolvedStudentId, cancellationToken);
            if (student is null)
            {
                return (false, BuildError("CREATE_CONVERSATION_DENIED", "Không tìm thấy học sinh."), null, false);
            }

            var existing = await _messagingRepository.GetDirectConversationAsync(currentUserId, request.ParticipantUserId, cancellationToken);
            if (existing is not null)
            {
                var detail = await BuildConversationDetailAsync(existing, currentUserId, cancellationToken);
                return (true, null, detail, false);
            }

            var now = VietnamTimeHelper.Now;

            var conversation = new Conversation
            {
                ConversationType = "DIRECT",
                StudentUserId = resolvedStudentId,
                CreatedByUserId = currentUserId,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _messagingRepository.AddConversationAsync(conversation, cancellationToken);
            await _messagingRepository.SaveChangesAsync(cancellationToken);

            var participants = new List<ConversationParticipant>
            {
                new()
                {
                    ConversationId = conversation.ConversationId,
                    UserId = currentUserId,
                    RoleInConversation = role,
                    JoinedAt = now,
                    IsPinned = false
                },
                new()
                {
                    ConversationId = conversation.ConversationId,
                    UserId = request.ParticipantUserId,
                    RoleInConversation = participantUser.Role,
                    JoinedAt = now,
                    IsPinned = false
                }
            };

            await _messagingRepository.AddParticipantsAsync(participants, cancellationToken);
            await _messagingRepository.SaveChangesAsync(cancellationToken);

            var created = await _messagingRepository.GetConversationByIdAsync(conversation.ConversationId, cancellationToken);
            if (created is null)
            {
                return (false, BuildError("CREATE_CONVERSATION_DENIED", "Không thể tạo cuộc trò chuyện."), null, false);
            }

            var createdDetail = await BuildConversationDetailAsync(created, currentUserId, cancellationToken);
            return (true, null, createdDetail, true);
        }

        public async Task<(bool Success, MessagingErrorDto? Error, ConversationDetailDto? Data)> GetConversationDetailAsync(
            int currentUserId,
            int conversationId,
            CancellationToken cancellationToken = default)
        {
            var conversation = await _messagingRepository.GetConversationByIdAsync(conversationId, cancellationToken);
            if (conversation is null)
            {
                return (false, BuildError("CONVERSATION_NOT_FOUND", "Không tìm thấy cuộc trò chuyện.", conversationId), null);
            }

            if (!conversation.Participants.Any(x => x.UserId == currentUserId))
            {
                return (false, BuildError("CONVERSATION_ACCESS_DENIED", "Bạn không có quyền truy cập cuộc trò chuyện.", conversationId), null);
            }

            var detail = await BuildConversationDetailAsync(conversation, currentUserId, cancellationToken);
            return (true, null, detail);
        }

        public async Task<(bool Success, MessagingErrorDto? Error, PagedResultDto<MessageItemDto>? Data)> GetMessagesAsync(
            int currentUserId,
            int conversationId,
            MessageListQueryDto query,
            CancellationToken cancellationToken = default)
        {
            if (!await _messagingRepository.IsParticipantAsync(conversationId, currentUserId, cancellationToken))
            {
                return (false, BuildError("MESSAGE_ACCESS_DENIED", "Bạn không có quyền truy cập tin nhắn.", conversationId), null);
            }

            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = query.PageSize <= 0 ? 30 : Math.Min(query.PageSize, 100);

            var (items, total) = await _messagingRepository.GetMessagesAsync(conversationId, page, pageSize, query.BeforeMessageId, cancellationToken);
            var participants = await _messagingRepository.GetParticipantsAsync(conversationId, cancellationToken);

            var mapped = items
                .OrderBy(x => x.MessageId)
                .Select(x => MapMessageItem(x, currentUserId, participants))
                .ToList();

            var result = new PagedResultDto<MessageItemDto>
            {
                Items = mapped,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize)
            };

            return (true, null, result);
        }

        public async Task<(bool Success, MessagingErrorDto? Error, ConversationReadDto? Data)> MarkConversationReadAsync(
            int currentUserId,
            int conversationId,
            MarkConversationReadRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var participant = await _messagingRepository.GetParticipantForUpdateAsync(conversationId, currentUserId, cancellationToken);
            if (participant is null)
            {
                return (false, BuildError("CONVERSATION_ACCESS_DENIED", "Bạn không có quyền truy cập cuộc trò chuyện.", conversationId), null);
            }

            var lastReadId = request.LastReadMessageId;
            if (lastReadId.HasValue)
            {
                var message = await _messagingRepository.GetMessageByIdAsync(lastReadId.Value, cancellationToken);
                if (message is null || message.ConversationId != conversationId)
                {
                    return (false, BuildError("INVALID_LAST_READ_MESSAGE", "Tin nhắn không hợp lệ.", conversationId), null);
                }
            }
            else
            {
                var latestMessage = await _messagingRepository.GetLatestMessageAsync(conversationId, cancellationToken);
                lastReadId = latestMessage?.MessageId;
            }

            var now = VietnamTimeHelper.Now;
            participant.LastReadMessageId = lastReadId;
            participant.LastReadAt = now;
            await _messagingRepository.SaveChangesAsync(cancellationToken);

            var readDto = new ConversationReadDto
            {
                ConversationId = conversationId,
                UserId = participant.UserId,
                FullName = participant.User.FullName,
                Role = participant.User.Role,
                LastReadMessageId = participant.LastReadMessageId,
                ReadAt = now
            };

            return (true, null, readDto);
        }

        public async Task<(bool Success, MessagingErrorDto? Error, MessageItemDto? Data, Dictionary<int, ConversationListItemDto> ConversationUpdates)> SendMessageAsync(
            int currentUserId,
            int conversationId,
            SendMessageRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var participant = await _messagingRepository.GetParticipantAsync(conversationId, currentUserId, cancellationToken);
            if (participant is null)
            {
                return (false, BuildError("MESSAGE_ACCESS_DENIED", "Bạn không có quyền gửi tin nhắn.", conversationId, request.ClientMessageId), null, new Dictionary<int, ConversationListItemDto>());
            }

            var messageType = string.IsNullOrWhiteSpace(request.MessageType) ? MessageTypeText : request.MessageType.Trim().ToUpperInvariant();
            if (messageType != MessageTypeText)
            {
                return (false, BuildError("INVALID_MESSAGE_TYPE", "Loại tin nhắn không hợp lệ.", conversationId, request.ClientMessageId), null, new Dictionary<int, ConversationListItemDto>());
            }

            var content = request.Content?.Trim();
            if (string.IsNullOrWhiteSpace(content))
            {
                return (false, BuildError("MESSAGE_CONTENT_REQUIRED", "Nội dung tin nhắn không được rỗng.", conversationId, request.ClientMessageId), null, new Dictionary<int, ConversationListItemDto>());
            }

            if (content.Length > 2000)
            {
                return (false, BuildError("MESSAGE_TOO_LONG", "Nội dung tin nhắn vượt quá 2000 ký tự.", conversationId, request.ClientMessageId), null, new Dictionary<int, ConversationListItemDto>());
            }

            var now = VietnamTimeHelper.Now;

            var message = new ChatMessage
            {
                ConversationId = conversationId,
                SenderUserId = currentUserId,
                Content = content,
                MessageType = messageType,
                ClientMessageId = request.ClientMessageId,
                SentAt = now,
                IsDeleted = false
            };

            await _messagingRepository.AddMessageAsync(message, cancellationToken);
            await _messagingRepository.SaveChangesAsync(cancellationToken);

            var conversation = await _messagingRepository.GetConversationForUpdateAsync(conversationId, cancellationToken);
            if (conversation is null)
            {
                return (false, BuildError("CONVERSATION_NOT_FOUND", "Không tìm thấy cuộc trò chuyện.", conversationId), null, new Dictionary<int, ConversationListItemDto>());
            }

            conversation.LastMessageId = message.MessageId;
            conversation.UpdatedAt = now;
            await _messagingRepository.SaveChangesAsync(cancellationToken);

            var fullMessage = await _messagingRepository.GetMessageByIdAsync(message.MessageId, cancellationToken);
            if (fullMessage is null)
            {
                return (false, BuildError("SEND_MESSAGE_FAILED", "Gửi tin nhắn thất bại.", conversationId, request.ClientMessageId), null, new Dictionary<int, ConversationListItemDto>());
            }

            var participants = await _messagingRepository.GetParticipantsAsync(conversationId, cancellationToken);
            var messageDto = MapMessageItem(fullMessage, currentUserId, participants);

            var updatedConversation = await _messagingRepository.GetConversationByIdAsync(conversationId, cancellationToken);
            var updates = new Dictionary<int, ConversationListItemDto>();
            if (updatedConversation is not null)
            {
                foreach (var p in updatedConversation.Participants)
                {
                    var otherUnreadCounts = await _messagingRepository.GetUnreadCountsAsync(new[] { conversationId }, p.UserId, cancellationToken);
                    updates[p.UserId] = MapConversationListItem(updatedConversation, p.UserId, otherUnreadCounts);
                }
            }

            return (true, null, messageDto, updates);
        }

        public async Task<(bool Success, MessagingErrorDto? Error, PagedResultDto<MessagingContactDto>? Data)> GetStudentContactsAsync(
            int nurseUserId,
            ConversationListQueryDto query,
            string? className,
            CancellationToken cancellationToken = default)
        {
            var user = await _messagingRepository.GetUserByIdAsync(nurseUserId, cancellationToken);
            if (user is null || user.Role != RoleNurse)
            {
                return (false, BuildError("ROLE_NOT_ALLOWED", "Bạn không có quyền truy cập danh sách học sinh."), null);
            }

            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);

            var (items, total) = await _messagingRepository.GetStudentContactsAsync(query.Keyword, className, page, pageSize, cancellationToken);
            var studentUserIds = items.Select(x => x.UserId).ToList();
            var conversationLookups = await _messagingRepository.GetDirectConversationLookupsAsync(nurseUserId, studentUserIds, cancellationToken);
            var lookupMap = conversationLookups.ToDictionary(x => x.OtherUserId, x => x);

            var mapped = items.Select(student =>
            {
                lookupMap.TryGetValue(student.UserId, out var lookup);

                return new MessagingContactDto
                {
                    UserId = student.UserId,
                    StudentId = student.UserId,
                    FullName = student.FullName,
                    ClassName = student.Class?.ClassName,
                    Role = RoleStudent,
                    AvatarUrl = student.User.Avatar,
                    Gender = student.User.Gender,
                    DateOfBirth = student.DateOfBirth,
                    HasConversation = lookup.ConversationId > 0,
                    ConversationId = lookup.ConversationId > 0 ? lookup.ConversationId : null,
                    LastMessageAt = lookup.LastMessageAt
                };
            }).ToList();

            var result = new PagedResultDto<MessagingContactDto>
            {
                Items = mapped,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize)
            };

            return (true, null, result);
        }

        public async Task<(bool Success, MessagingErrorDto? Error, IReadOnlyList<MessagingContactDto>? Data)> GetNurseContactsAsync(
            int studentUserId,
            string? keyword,
            CancellationToken cancellationToken = default)
        {
            var user = await _messagingRepository.GetUserByIdAsync(studentUserId, cancellationToken);
            if (user is null || user.Role != RoleStudent)
            {
                return (false, BuildError("ROLE_NOT_ALLOWED", "Bạn không có quyền truy cập danh sách y tá."), null);
            }

            var nurses = await _messagingRepository.GetNurseContactsAsync(keyword, cancellationToken);
            var nurseIds = nurses.Select(x => x.UserId).ToList();
            var conversationLookups = await _messagingRepository.GetDirectConversationLookupsAsync(studentUserId, nurseIds, cancellationToken);
            var lookupMap = conversationLookups.ToDictionary(x => x.OtherUserId, x => x);

            var mapped = nurses.Select(nurse =>
            {
                lookupMap.TryGetValue(nurse.UserId, out var lookup);

                return new MessagingContactDto
                {
                    UserId = nurse.UserId,
                    FullName = nurse.FullName,
                    Role = RoleNurse,
                    AvatarUrl = nurse.Avatar,
                    Email = nurse.Email,
                    PhoneNumber = nurse.Phone,
                    HasConversation = lookup.ConversationId > 0,
                    ConversationId = lookup.ConversationId > 0 ? lookup.ConversationId : null,
                    LastMessageAt = lookup.LastMessageAt
                };
            }).ToList();

            return (true, null, mapped);
        }

        public async Task<(bool Success, MessagingErrorDto? Error, ConversationParticipantDto? User)> GetUserSummaryAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _messagingRepository.GetUserByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                return (false, BuildError("UNAUTHORIZED", "Không tìm thấy người dùng."), null);
            }

            return (true, null, new ConversationParticipantDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Role = user.Role,
                AvatarUrl = user.Avatar
            });
        }

        public async Task<(bool Success, MessagingErrorDto? Error)> EnsureParticipantAsync(
            int conversationId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var ok = await _messagingRepository.IsParticipantAsync(conversationId, userId, cancellationToken);
            if (!ok)
            {
                return (false, BuildError("CONVERSATION_ACCESS_DENIED", "Bạn không có quyền truy cập cuộc trò chuyện.", conversationId));
            }

            return (true, null);
        }

        private static bool IsMessagingRole(string role) => role == RoleNurse || role == RoleStudent;

        private static bool IsValidPair(string currentRole, string participantRole)
        {
            return (currentRole == RoleNurse && participantRole == RoleStudent) ||
                   (currentRole == RoleStudent && participantRole == RoleNurse);
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

        private ConversationListItemDto MapConversationListItem(Conversation conversation, int currentUserId, Dictionary<int, int> unreadCounts)
        {
            var currentParticipant = conversation.Participants.FirstOrDefault(x => x.UserId == currentUserId);
            var otherParticipant = conversation.Participants.FirstOrDefault(x => x.UserId != currentUserId);

            var title = conversation.Title;
            var avatarUrl = otherParticipant?.User.Avatar;
            if (conversation.ConversationType == "DIRECT")
            {
                if (conversation.Student != null && conversation.Student.UserId != currentUserId)
                {
                    title = conversation.Student.FullName;
                    avatarUrl = conversation.Student.User.Avatar;
                }
                else if (otherParticipant != null)
                {
                    title = otherParticipant.User.FullName;
                    avatarUrl = otherParticipant.User.Avatar;
                }
                else if (conversation.Student != null)
                {
                    title = conversation.Student.FullName;
                    avatarUrl = conversation.Student.User.Avatar;
                }
            }

            var unreadCount = 0;
            if (unreadCounts.TryGetValue(conversation.ConversationId, out var count))
            {
                unreadCount = count;
            }

            return new ConversationListItemDto
            {
                ConversationId = conversation.ConversationId,
                ConversationType = conversation.ConversationType,
                Title = title,
                StudentId = conversation.Student?.UserId,
                StudentName = conversation.Student?.FullName,
                ClassName = conversation.Student?.Class?.ClassName,
                AvatarUrl = avatarUrl,
                Participants = conversation.Participants.Select(MapParticipant).ToList(),
                LastMessage = conversation.LastMessage != null ? MapMessageItem(conversation.LastMessage, currentUserId, conversation.Participants.ToList()) : null,
                UnreadCount = unreadCount,
                IsPinned = currentParticipant?.IsPinned ?? false,
                UpdatedAt = conversation.UpdatedAt,
                CreatedAt = conversation.CreatedAt
            };
        }

        private async Task<ConversationDetailDto> BuildConversationDetailAsync(Conversation conversation, int currentUserId, CancellationToken cancellationToken)
        {
            var currentParticipant = conversation.Participants.First(x => x.UserId == currentUserId);
            var unreadCount = await _messagingRepository.GetUnreadCountAsync(conversation.ConversationId, currentUserId, currentParticipant.LastReadMessageId, cancellationToken);

            var otherParticipant = conversation.Participants.FirstOrDefault(x => x.UserId != currentUserId);

            var title = conversation.Title;
            var avatarUrl = otherParticipant?.User.Avatar;
            if (conversation.ConversationType == "DIRECT")
            {
                if (conversation.Student != null && conversation.Student.UserId != currentUserId)
                {
                    title = conversation.Student.FullName;
                    avatarUrl = conversation.Student.User.Avatar;
                }
                else if (otherParticipant != null)
                {
                    title = otherParticipant.User.FullName;
                    avatarUrl = otherParticipant.User.Avatar;
                }
                else if (conversation.Student != null)
                {
                    title = conversation.Student.FullName;
                    avatarUrl = conversation.Student.User.Avatar;
                }
            }

            return new ConversationDetailDto
            {
                ConversationId = conversation.ConversationId,
                ConversationType = conversation.ConversationType,
                Title = title,
                StudentId = conversation.Student?.UserId,
                StudentName = conversation.Student?.FullName,
                ClassName = conversation.Student?.Class?.ClassName,
                AvatarUrl = avatarUrl,
                Participants = conversation.Participants.Select(MapParticipant).ToList(),
                LastMessage = conversation.LastMessage != null ? MapMessageItem(conversation.LastMessage, currentUserId, conversation.Participants.ToList()) : null,
                UnreadCount = unreadCount,
                IsPinned = currentParticipant.IsPinned,
                UpdatedAt = conversation.UpdatedAt,
                CreatedAt = conversation.CreatedAt
            };
        }

        private MessageItemDto MapMessageItem(ChatMessage message, int currentUserId, IReadOnlyList<ConversationParticipant> participants)
        {
            var readBy = participants
                .Where(p => p.UserId != message.SenderUserId && p.LastReadMessageId.HasValue && p.LastReadMessageId.Value >= message.MessageId && p.LastReadAt.HasValue)
                .Select(p => new ConversationReadDto
                {
                    ConversationId = message.ConversationId,
                    UserId = p.UserId,
                    FullName = p.User.FullName,
                    Role = p.User.Role,
                    LastReadMessageId = p.LastReadMessageId,
                    ReadAt = p.LastReadAt!.Value
                })
                .ToList();

            return new MessageItemDto
            {
                MessageId = message.MessageId,
                ConversationId = message.ConversationId,
                SenderId = message.SenderUserId,
                SenderName = message.Sender.FullName,
                SenderRole = message.Sender.Role,
                SenderAvatarUrl = message.Sender.Avatar,
                Content = message.Content,
                MessageType = message.MessageType,
                ClientMessageId = message.ClientMessageId,
                SentAt = message.SentAt,
                EditedAt = message.EditedAt,
                DeletedAt = message.DeletedAt,
                IsDeleted = message.IsDeleted,
                IsMine = message.SenderUserId == currentUserId,
                ReadBy = readBy,
                Attachments = message.Attachments.Select(a => new MessageAttachmentDto
                {
                    AttachmentId = a.AttachmentId,
                    FileName = a.FileName,
                    OriginalFileName = a.OriginalFileName,
                    FileUrl = a.FileUrl,
                    ContentType = a.ContentType,
                    SizeBytes = a.SizeBytes
                }).ToList()
            };
        }

        private static ConversationParticipantDto MapParticipant(ConversationParticipant participant)
        {
            return new ConversationParticipantDto
            {
                UserId = participant.UserId,
                FullName = participant.User.FullName,
                Role = participant.User.Role,
                AvatarUrl = participant.User.Avatar
            };
        }
    }
}
