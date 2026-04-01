using EduHealth.Data.Entities;
using EduHealth.DTOs.Notifications;
using EduHealth.Repositories.Interfaces;
using EduHealth.Services.Interfaces;

namespace EduHealth.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationService(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<NotificationRecipientsPreviewResponseDto> PreviewRecipientsAsync(
            NotificationRecipientsPreviewRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var students = await ResolveRecipientsAsync(request.ClassId, request.UserIds, cancellationToken);

            var data = students.Select(x => new NotificationRecipientPreviewItemDto
            {
                UserId = x.UserId,
                FullName = x.FullName,
                ClassId = x.ClassId,
                ClassName = x.Class.ClassName
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
            var students = await ResolveRecipientsAsync(request.ClassId, request.RecipientUserIds, cancellationToken);

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

            var recipients = students
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

            await _notificationRepository.AddRecipientsAsync(recipients, cancellationToken);
            await _notificationRepository.SaveChangesAsync(cancellationToken);

            return new CreateNotificationResponseDto
            {
                NotificationId = notification.NotificationId,
                TotalRecipients = recipients.Count
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
    }
}
