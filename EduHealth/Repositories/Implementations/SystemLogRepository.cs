using EduHealth.Data;
using EduHealth.Data.Entities;
using EduHealth.Repositories.Interfaces;

namespace EduHealth.Repositories.Implementations
{
    public sealed class SystemLogRepository : ISystemLogRepository
    {
        private readonly AppDbContext _context;

        public SystemLogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(SystemLog log, CancellationToken cancellationToken)
        {
            _context.SystemLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
