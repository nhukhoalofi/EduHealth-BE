using EduHealth.Data.Entities;
using EduHealth.Repositories.Interfaces;
using EduHealth.Services.Interfaces;
using EduHealth.Services.Models;

namespace EduHealth.Services.Implementations
{
    public sealed class NotificationTargetResolver : INotificationTargetResolver
    {
        public const string TargetModeClass = "CLASS";
        public const string TargetModeUsers = "USERS";
        public const string TargetModeRoles = "ROLES";
        public const string TargetModeNone = "NONE";

        private static readonly HashSet<string> ValidTargetModes = new(StringComparer.OrdinalIgnoreCase)
        {
            TargetModeClass,
            TargetModeUsers,
            TargetModeRoles,
            TargetModeNone
        };

        private static readonly HashSet<string> AllowedRecipientRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "ADMIN",
            "NURSE",
            "STUDENT"
        };

        private readonly INotificationRepository _notificationRepository;

        public NotificationTargetResolver(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public string ResolveTargetMode(
            string? targetMode,
            int? classId,
            IReadOnlyList<int>? recipientUserIds,
            IReadOnlyList<string>? targetRoles)
        {
            if (!string.IsNullOrWhiteSpace(targetMode))
            {
                return targetMode.Trim().ToUpperInvariant();
            }

            if (classId.HasValue && classId.Value > 0)
            {
                return TargetModeClass;
            }

            if (recipientUserIds is { Count: > 0 })
            {
                return TargetModeUsers;
            }

            if (targetRoles is { Count: > 0 })
            {
                return TargetModeRoles;
            }

            return TargetModeNone;
        }

        public bool IsValidTargetMode(string? targetMode)
        {
            return !string.IsNullOrWhiteSpace(targetMode) && ValidTargetModes.Contains(targetMode);
        }

        public bool IsValidTargetRole(string? role)
        {
            return IsAllowedRecipientRole(role);
        }

        public bool IsAllowedRecipientRole(string? role)
        {
            return !string.IsNullOrWhiteSpace(role) && AllowedRecipientRoles.Contains(role);
        }

        public List<int> NormalizeRecipientUserIds(IReadOnlyList<int>? userIds)
        {
            return userIds?
                .Where(x => x > 0)
                .Distinct()
                .ToList() ?? new List<int>();
        }

        public List<string> NormalizeTargetRoles(IReadOnlyList<string>? roles)
        {
            return roles?
                .Select(x => (x ?? string.Empty).Trim().ToUpperInvariant())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList() ?? new List<string>();
        }

        public async Task<List<User>> ResolveRecipientsAsync(
            NotificationTargetResolveRequest request,
            CancellationToken cancellationToken = default)
        {
            var targetMode = ResolveTargetMode(
                request.TargetMode,
                request.ClassId,
                request.RecipientUserIds,
                request.TargetRoles);

            var users = targetMode switch
            {
                TargetModeClass => await ResolveClassRecipientsAsync(request.ClassId, cancellationToken),
                TargetModeUsers => await ResolveUserRecipientsAsync(request.RecipientUserIds, cancellationToken),
                TargetModeRoles => await ResolveRoleRecipientsAsync(request.TargetRoles, cancellationToken),
                _ => new List<User>()
            };

            return DistinctAllowedRecipients(users);
        }

        public async Task<List<User>> ResolvePreviewRecipientsAsync(
            int? classId,
            IReadOnlyList<int>? userIds,
            CancellationToken cancellationToken = default)
        {
            var users = new List<User>();

            if (userIds is { Count: > 0 })
            {
                users.AddRange(await ResolveRecipientsAsync(new NotificationTargetResolveRequest
                {
                    TargetMode = TargetModeUsers,
                    RecipientUserIds = userIds
                }, cancellationToken));
            }

            if (classId.HasValue && classId.Value > 0)
            {
                users.AddRange(await ResolveRecipientsAsync(new NotificationTargetResolveRequest
                {
                    TargetMode = TargetModeClass,
                    ClassId = classId
                }, cancellationToken));
            }

            return DistinctAllowedRecipients(users);
        }

        private async Task<List<User>> ResolveClassRecipientsAsync(
            int? classId,
            CancellationToken cancellationToken)
        {
            if (!classId.HasValue || classId.Value <= 0)
            {
                return new List<User>();
            }

            var students = await _notificationRepository.GetRecipientsByClassIdAsync(classId.Value, cancellationToken);

            return students
                .Where(x => x.User is not null && string.Equals(x.User.Role, "STUDENT", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.User)
                .ToList();
        }

        private async Task<List<User>> ResolveUserRecipientsAsync(
            IReadOnlyList<int>? userIds,
            CancellationToken cancellationToken)
        {
            var normalized = NormalizeRecipientUserIds(userIds);
            if (normalized.Count == 0)
            {
                return new List<User>();
            }

            return await _notificationRepository.GetUsersByIdsAsync(normalized, cancellationToken);
        }

        private async Task<List<User>> ResolveRoleRecipientsAsync(
            IReadOnlyList<string>? roles,
            CancellationToken cancellationToken)
        {
            var normalized = NormalizeTargetRoles(roles)
                .Where(IsAllowedRecipientRole)
                .ToList();

            if (normalized.Count == 0)
            {
                return new List<User>();
            }

            return await _notificationRepository.GetUsersByRolesAsync(normalized, cancellationToken);
        }

        private List<User> DistinctAllowedRecipients(IEnumerable<User> users)
        {
            var map = new Dictionary<int, User>();

            foreach (var user in users)
            {
                if (user.UserId <= 0 || !IsAllowedRecipientRole(user.Role))
                {
                    continue;
                }

                map.TryAdd(user.UserId, user);
            }

            return map.Values.ToList();
        }
    }
}
