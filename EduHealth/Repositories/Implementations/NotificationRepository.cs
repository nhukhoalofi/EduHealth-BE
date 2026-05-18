using EduHealth.Data;
using EduHealth.Data.Entities;
using EduHealth.Repositories.Interfaces;
using EduHealth.Helpers;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Repositories.Implementations
{
    public class NotificationRepository : INotificationRepository
    {
        private static readonly string[] AllowedRecipientRoles = { "ADMIN", "NURSE", "STUDENT" };

        private readonly AppDbContext _context;

        public NotificationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Student>> GetRecipientsByClassIdAsync(int classId, CancellationToken cancellationToken = default)
        {
            return await _context.Students
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Class)
                .Where(x => x.ClassId == classId && x.User.IsActive && x.User.Role == "STUDENT")
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Student>> GetRecipientsByUserIdsAsync(IReadOnlyList<int> userIds, CancellationToken cancellationToken = default)
        {
            var normalized = userIds.Distinct().ToList();

            return await _context.Students
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Class)
                .Where(x => normalized.Contains(x.UserId) && x.User.IsActive && x.User.Role == "STUDENT")
                .ToListAsync(cancellationToken);
        }

        public async Task<List<User>> GetUsersByIdsAsync(IReadOnlyList<int> userIds, CancellationToken cancellationToken = default)
        {
            var normalized = userIds.Distinct().ToList();

            return await _context.Users
                .AsNoTracking()
                .Where(x => normalized.Contains(x.UserId) && x.IsActive && AllowedRecipientRoles.Contains(x.Role))
                .ToListAsync(cancellationToken);
        }

        public async Task<List<User>> GetUsersByRolesAsync(IReadOnlyList<string> roles, CancellationToken cancellationToken = default)
        {
            var normalized = roles
                .Select(x => (x ?? string.Empty).Trim().ToUpperInvariant())
                .Where(x => AllowedRecipientRoles.Contains(x))
                .Distinct()
                .ToList();

            return await _context.Users
                .AsNoTracking()
                .Where(x => normalized.Contains(x.Role) && x.IsActive && AllowedRecipientRoles.Contains(x.Role))
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ClassExistsAsync(int classId, CancellationToken cancellationToken = default)
        {
            return await _context.SchoolClasses
                .AsNoTracking()
                .AnyAsync(x => x.ClassId == classId, cancellationToken);
        }

        public async Task AddNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            await _context.Notifications.AddAsync(notification, cancellationToken);
        }

        public async Task AddRecipientsAsync(IReadOnlyList<NotificationRecipient> recipients, CancellationToken cancellationToken = default)
        {
            await _context.NotificationRecipients.AddRangeAsync(recipients, cancellationToken);
        }

        public async Task<bool> RecipientExistsAsync(int userId, int notificationId, CancellationToken cancellationToken = default)
        {
            return await _context.NotificationRecipients
                .AsNoTracking()
                .AnyAsync(x => x.UserId == userId && x.NotificationId == notificationId, cancellationToken);
        }

        public async Task<NotificationRecipient?> GetRecipientAsync(int userId, int notificationId, CancellationToken cancellationToken = default)
        {
            return await _context.NotificationRecipients
                .FirstOrDefaultAsync(x => x.UserId == userId && x.NotificationId == notificationId, cancellationToken);
        }

        public async Task<(List<NotificationRecipient> Items, int Total)> GetNotificationsForUserAsync(
            int userId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.NotificationRecipients
                .AsNoTracking()
                .Include(x => x.Notification)
                    .ThenInclude(n => n.CreatedByUser)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Notification.CreatedAt);

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task<(List<Notification> Items, int Total)> GetSentNotificationsAsync(
            int createdByUserId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Notifications
                .AsNoTracking()
                .Include(x => x.Recipients)
                .Where(x => x.CreatedByUserId == createdByUserId)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.NotificationId);

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task<(List<Notification> Items, int Total)> GetPublicNotificationsAsync(
            string? type,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Notifications
                .AsNoTracking()
                .Where(x => x.Status == "PUBLISHED" && (x.Visibility == "PUBLIC" || x.Visibility == "BOTH"));

            if (!string.IsNullOrWhiteSpace(type))
            {
                var normalizedType = type.Trim().ToUpperInvariant();
                query = query.Where(x => x.Type == normalizedType);
            }

            query = query
                .OrderByDescending(x => x.PublishedAt ?? x.CreatedAt)
                .ThenByDescending(x => x.NotificationId);

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.NotificationRecipients
                .AsNoTracking()
                .Where(x => x.UserId == userId && !x.IsRead)
                .CountAsync(cancellationToken);
        }

        public async Task<int> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default)
        {
            var now = VietnamTimeHelper.Now;

            return await _context.NotificationRecipients
                .Where(x => x.UserId == userId && !x.IsRead)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.IsRead, true)
                    .SetProperty(x => x.ReadAt, now),
                    cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
