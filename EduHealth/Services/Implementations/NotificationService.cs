using EduHealth.Data.Entities;
using EduHealth.DTOs.Notifications;
using EduHealth.Repositories.Interfaces;
using EduHealth.Services.Interfaces;

namespace EduHealth.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ISystemLogWriter _logWriter;
        private readonly ISseNotificationService _sseService;

        public NotificationService(INotificationRepository notificationRepository, ISystemLogWriter logWriter, ISseNotificationService sseService)
        {
            _notificationRepository = notificationRepository;
            _logWriter = logWriter;
            _sseService = sseService;
        }

        public async Task<NotificationRecipientsPreviewResponseDto> PreviewRecipientsAsync(
            NotificationRecipientsPreviewRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var recipients = await ResolveAllRecipientsAsync(request.ClassId, request.UserIds, cancellationToken);

            // Get student info for those who are students
            var studentUserIds = recipients.Select(x => x.UserId).ToList();
            var studentInfo = await _notificationRepository.GetRecipientsByUserIdsAsync(studentUserIds, cancellationToken);
            var studentMap = studentInfo.ToDictionary(s => s.UserId, s => s);

            var data = recipients.Select(x => 
            {
                studentMap.TryGetValue(x.UserId, out var student);

                return new NotificationRecipientPreviewItemDto
                {
                    UserId = x.UserId,
                    FullName = x.FullName,
                    Role = x.Role,
                    ClassId = student?.ClassId,
                    ClassName = student?.Class?.ClassName
                };
            }).ToList();

            return new NotificationRecipientsPreviewResponseDto
            {
                Total = data.Count,
                Recipients = data
            };
        }

        public async Task<CreateNotificationResponseDto> CreateAsync(
            int createdByUserId,
            CreateNotificationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var recipients = await ResolveAllRecipientsAsync(request.ClassId, request.RecipientUserIds, cancellationToken);

            var notification = new Notification
            {
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                Type = request.Type.Trim(),
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow,
                ClassId = request.ClassId,
                DiseaseId = request.DiseaseId,
                VaccinationId = request.VaccinationId
            };

            await _notificationRepository.AddNotificationAsync(notification, cancellationToken);
            await _notificationRepository.SaveChangesAsync(cancellationToken);

            var recipientList = recipients
                .Select(x => new NotificationRecipient
                {
                    NotificationId = notification.NotificationId,
                    UserId = x.UserId,
                    IsRead = false,
                    ReadAt = null,
                    SentAt = DateTime.UtcNow,
                    Status = "SENT"
                })
                .ToList();

            await _notificationRepository.AddRecipientsAsync(recipientList, cancellationToken);
            await _notificationRepository.SaveChangesAsync(cancellationToken);

            await _logWriter.WriteAsync(new SystemLogWriteRequest
            {
                ActorUserId = createdByUserId,
                Module = "NOTIFICATIONS",
                Action = "SEND_NOTIFICATION",
                TargetType = "Notification",
                TargetId = notification.NotificationId.ToString(),
                TargetLabel = notification.Title,
                Description = $"Gửi thông báo: {notification.Title}",
                Status = "SUCCESS",
                Metadata = new { recipientCount = recipientList.Count, title = notification.Title }
            }, cancellationToken);

            // Broadcast to SSE clients
            var recipientUserIds = recipientList.Select(r => r.UserId).ToArray();
            _ = _sseService.BroadcastNotificationCreatedAsync(notification.NotificationId, recipientUserIds, cancellationToken);

            return new CreateNotificationResponseDto
            {
                NotificationId = notification.NotificationId,
                TotalRecipients = recipientList.Count
            };
        }

        public async Task<bool> MarkReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default)
        {
            var recipient = await _notificationRepository.GetRecipientAsync(userId, notificationId, cancellationToken);

            if (recipient is null)
            {
                return false;
            }

            if (!recipient.IsRead)
            {
                recipient.IsRead = true;
                recipient.ReadAt = DateTime.UtcNow;
                await _notificationRepository.SaveChangesAsync(cancellationToken);

                // Broadcast SSE event
                _ = _sseService.BroadcastNotificationReadAsync(userId, notificationId, cancellationToken);
            }

            return true;
        }

        private async Task<List<Student>> ResolveRecipientsAsync(
            int? classId,
            IReadOnlyList<int>? userIds,
            CancellationToken cancellationToken)
        {
            if (userIds is { Count: > 0 })
            {
                return await _notificationRepository.GetRecipientsByUserIdsAsync(userIds, cancellationToken);
            }

            if (classId.HasValue && classId.Value > 0)
            {
                return await _notificationRepository.GetRecipientsByClassIdAsync(classId.Value, cancellationToken);
            }

            return new List<Student>();
        }

        private async Task<List<User>> ResolveAllRecipientsAsync(
            int? classId,
            IReadOnlyList<int>? userIds,
            CancellationToken cancellationToken)
        {
            var userSet = new HashSet<int>();

            // Collect userIds from direct userIds parameter
            if (userIds is { Count: > 0 })
            {
                foreach (var uid in userIds.Distinct())
                {
                    userSet.Add(uid);
                }
            }

            // Collect userIds from class
            if (classId.HasValue && classId.Value > 0)
            {
                var studentIds = await _notificationRepository.GetRecipientsByClassIdAsync(classId.Value, cancellationToken);
                foreach (var s in studentIds)
                {
                    userSet.Add(s.UserId);
                }
            }

            // Fetch all users
            if (userSet.Count == 0)
            {
                return new List<User>();
            }

            return await _notificationRepository.GetUsersByIdsAsync(userSet.ToList(), cancellationToken);
        }
    }
}
