using EduHealth.Data.Entities;
using EduHealth.Services.Models;

namespace EduHealth.Services.Interfaces
{
    public interface INotificationTargetResolver
    {
        string ResolveTargetMode(
            string? targetMode,
            int? classId,
            IReadOnlyList<int>? recipientUserIds,
            IReadOnlyList<string>? targetRoles);

        bool IsValidTargetMode(string? targetMode);
        bool IsValidTargetRole(string? role);
        bool IsAllowedRecipientRole(string? role);

        List<int> NormalizeRecipientUserIds(IReadOnlyList<int>? userIds);
        List<string> NormalizeTargetRoles(IReadOnlyList<string>? roles);

        Task<List<User>> ResolveRecipientsAsync(
            NotificationTargetResolveRequest request,
            CancellationToken cancellationToken = default);

        Task<List<User>> ResolvePreviewRecipientsAsync(
            int? classId,
            IReadOnlyList<int>? userIds,
            CancellationToken cancellationToken = default);
    }
}
