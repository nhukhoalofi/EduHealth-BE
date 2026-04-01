using EduHealth.Data;
using EduHealth.Data.Entities;
using EduHealth.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Repositories.Implementations
{
    public class NotificationRepository : INotificationRepository
    {
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
                .Where(x => x.ClassId == classId && x.User.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Student>> GetRecipientsByUserIdsAsync(IReadOnlyList<int> userIds, CancellationToken cancellationToken = default)
        {
            var normalized = userIds.Distinct().ToList();

            return await _context.Students
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Class)
                .Where(x => normalized.Contains(x.UserId) && x.User.IsActive)
                .ToListAsync(cancellationToken);
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

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
