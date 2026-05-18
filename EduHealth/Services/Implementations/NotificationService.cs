using EduHealth.Data.Entities;
using EduHealth.DTOs.Notifications;
using EduHealth.Repositories.Interfaces;
using EduHealth.Services.Interfaces;
using EduHealth.Services.Models;
using EduHealth.Helpers;

namespace EduHealth.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private const string VisibilityInternal = "INTERNAL";
        private const string VisibilityPublic = "PUBLIC";
        private const string VisibilityBoth = "BOTH";

        private const string TargetModeClass = "CLASS";
        private const string TargetModeUsers = "USERS";
        private const string TargetModeRoles = "ROLES";
        private const string TargetModeNone = "NONE";

        private static readonly HashSet<string> ValidVisibilities = new(StringComparer.OrdinalIgnoreCase)
        {
            VisibilityInternal,
            VisibilityPublic,
            VisibilityBoth
        };

        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationTargetResolver _targetResolver;
        private readonly ISystemLogWriter _logWriter;
        private readonly ISseNotificationService _sseService;

        public NotificationService(
            INotificationRepository notificationRepository,
            INotificationTargetResolver targetResolver,
            ISystemLogWriter logWriter,
            ISseNotificationService sseService)
        {
            _notificationRepository = notificationRepository;
            _targetResolver = targetResolver;
            _logWriter = logWriter;
            _sseService = sseService;
        }

        public async Task<NotificationRecipientsPreviewResponseDto> PreviewRecipientsAsync(
            NotificationRecipientsPreviewRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var recipients = await _targetResolver.ResolvePreviewRecipientsAsync(request.ClassId, request.UserIds, cancellationToken);

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

        public async Task<(bool Success, string Message, string? Field)> ValidateCreateAsync(
            CreateNotificationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var visibility = NormalizeVisibility(request.Visibility);
            if (!ValidVisibilities.Contains(visibility))
            {
                return (false, "visibility không hợp lệ.", "visibility");
            }

            var targetMode = _targetResolver.ResolveTargetMode(
                request.TargetMode,
                request.ClassId,
                request.RecipientUserIds,
                request.TargetRoles);
            if (!_targetResolver.IsValidTargetMode(targetMode))
            {
                return (false, "targetMode không hợp lệ.", "targetMode");
            }

            if ((visibility == VisibilityInternal || visibility == VisibilityBoth) && targetMode == TargetModeNone)
            {
                return (false, "INTERNAL/BOTH phải chọn targetMode khác NONE.", "targetMode");
            }

            switch (targetMode)
            {
                case TargetModeNone:
                    return (true, string.Empty, null);

                case TargetModeClass:
                    if (!request.ClassId.HasValue || request.ClassId.Value <= 0)
                    {
                        return (false, "CLASS bắt buộc classId.", "classId");
                    }

                    if (!await _notificationRepository.ClassExistsAsync(request.ClassId.Value, cancellationToken))
                    {
                        return (false, "classId không tồn tại.", "classId");
                    }

                    return (true, string.Empty, null);

                case TargetModeUsers:
                    var requestedUserIds = _targetResolver.NormalizeRecipientUserIds(request.RecipientUserIds);
                    if (requestedUserIds.Count == 0)
                    {
                        return (false, "USERS bắt buộc recipientUserIds không rỗng.", "recipientUserIds");
                    }

                    if (request.RecipientUserIds?.Any(x => x <= 0) == true)
                    {
                        return (false, "recipientUserIds không hợp lệ.", "recipientUserIds");
                    }

                    var users = await _targetResolver.ResolveRecipientsAsync(new NotificationTargetResolveRequest
                    {
                        TargetMode = targetMode,
                        RecipientUserIds = request.RecipientUserIds
                    }, cancellationToken);
                    if (users.Count != requestedUserIds.Count)
                    {
                        return (false, "recipientUserIds chứa người dùng không tồn tại, không hoạt động hoặc role không hợp lệ.", "recipientUserIds");
                    }

                    return (true, string.Empty, null);

                case TargetModeRoles:
                    var roles = _targetResolver.NormalizeTargetRoles(request.TargetRoles);
                    if (roles.Count == 0)
                    {
                        return (false, "ROLES bắt buộc targetRoles không rỗng.", "targetRoles");
                    }

                    if (roles.Any(x => !_targetResolver.IsValidTargetRole(x)))
                    {
                        return (false, "targetRoles chỉ nhận ADMIN, NURSE, STUDENT.", "targetRoles");
                    }

                    return (true, string.Empty, null);

                default:
                    return (false, "targetMode không hợp lệ.", "targetMode");
            }
        }

        public async Task<CreateNotificationResponseDto> CreateAsync(
            int createdByUserId,
            CreateNotificationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var visibility = NormalizeVisibility(request.Visibility);
            var targetMode = _targetResolver.ResolveTargetMode(
                request.TargetMode,
                request.ClassId,
                request.RecipientUserIds,
                request.TargetRoles);
            var recipients = await _targetResolver.ResolveRecipientsAsync(new NotificationTargetResolveRequest
            {
                TargetMode = targetMode,
                ClassId = request.ClassId,
                RecipientUserIds = request.RecipientUserIds,
                TargetRoles = request.TargetRoles
            }, cancellationToken);
            var now = VietnamTimeHelper.Now;

            var notification = new Notification
            {
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                Image = request.Image?.Trim(),
                Type = request.Type.Trim(),
                Visibility = visibility,
                Status = "PUBLISHED",
                CreatedByUserId = createdByUserId,
                CreatedAt = now,
                PublishedAt = now,
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
                    SentAt = now,
                    Status = "SENT"
                })
                .ToList();

            if (recipientList.Count > 0)
            {
                await _notificationRepository.AddRecipientsAsync(recipientList, cancellationToken);
                await _notificationRepository.SaveChangesAsync(cancellationToken);
            }

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
                recipient.ReadAt = VietnamTimeHelper.Now;
                await _notificationRepository.SaveChangesAsync(cancellationToken);

                // Broadcast SSE event
                _ = _sseService.BroadcastNotificationReadAsync(userId, notificationId, cancellationToken);
            }

            return true;
        }

        public async Task<GetNotificationsResponseDto> GetNotificationsAsync(
            int userId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            var (items, total) = await _notificationRepository.GetNotificationsForUserAsync(userId, page, pageSize, cancellationToken);
            var unreadCount = await _notificationRepository.GetUnreadCountAsync(userId, cancellationToken);

            var dtoItems = items.Select(r => new NotificationItemDto
            {
                NotificationId = r.NotificationId,
                Title = r.Notification.Title,
                Content = r.Notification.Content,
                Image = r.Notification.Image,
                Type = r.Notification.Type,
                CreatedAt = r.Notification.CreatedAt,
                CreatedByUserName = r.Notification.CreatedByUser?.FullName ?? "Hệ thống",
                ClassId = r.Notification.ClassId,
                DiseaseId = r.Notification.DiseaseId,
                VaccinationId = r.Notification.VaccinationId,
                IsRead = r.IsRead,
                ReadAt = r.ReadAt
            }).ToList();

            return new GetNotificationsResponseDto
            {
                Items = dtoItems,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                UnreadCount = unreadCount
            };
        }

        public async Task<PublicNotificationsResponseDto> GetPublicNotificationsAsync(
            int page,
            int pageSize,
            string? type,
            CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 6;
            if (pageSize > 50) pageSize = 50;

            var normalizedType = string.IsNullOrWhiteSpace(type)
                ? null
                : type.Trim().ToUpperInvariant();

            var (items, total) = await _notificationRepository.GetPublicNotificationsAsync(
                normalizedType,
                page,
                pageSize,
                cancellationToken);

            var dtoItems = items.Select(x => new PublicNotificationItemDto
            {
                NotificationId = x.NotificationId,
                Title = x.Title,
                Content = x.Content,
                Image = x.Image,
                Type = x.Type,
                CreatedAt = x.CreatedAt,
                PublishedAt = x.PublishedAt,
                ClassId = x.ClassId,
                DiseaseId = x.DiseaseId,
                VaccinationId = x.VaccinationId
            }).ToList();

            return new PublicNotificationsResponseDto
            {
                Items = dtoItems,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };
        }

        public async Task<SentNotificationsResponseDto> GetSentNotificationsAsync(
            int createdByUserId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            var (items, total) = await _notificationRepository.GetSentNotificationsAsync(
                createdByUserId,
                page,
                pageSize,
                cancellationToken);

            var dtoItems = items.Select(x =>
            {
                var readCount = x.Recipients.Count(r => r.IsRead);
                var totalRecipients = x.Recipients.Count;

                return new SentNotificationItemDto
                {
                    NotificationId = x.NotificationId,
                    Title = x.Title,
                    Content = x.Content,
                    Image = x.Image,
                    Type = x.Type,
                    Visibility = x.Visibility,
                    CreatedAt = x.CreatedAt,
                    ClassId = x.ClassId,
                    DiseaseId = x.DiseaseId,
                    VaccinationId = x.VaccinationId,
                    TotalRecipients = totalRecipients,
                    ReadCount = readCount,
                    UnreadCount = totalRecipients - readCount
                };
            }).ToList();

            return new SentNotificationsResponseDto
            {
                Items = dtoItems,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };
        }

        public async Task<int> MarkAllReadAsync(int userId, CancellationToken cancellationToken = default)
        {
            var count = await _notificationRepository.MarkAllAsReadAsync(userId, cancellationToken);

            if (count > 0)
            {
                _ = _sseService.BroadcastAllNotificationsReadAsync(userId, cancellationToken);
            }

            return count;
        }

        private static string NormalizeVisibility(string? visibility)
        {
            return string.IsNullOrWhiteSpace(visibility)
                ? VisibilityInternal
                : visibility.Trim().ToUpperInvariant();
        }
    }
}
