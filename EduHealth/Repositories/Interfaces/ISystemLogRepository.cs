using EduHealth.Data.Entities;

namespace EduHealth.Repositories.Interfaces
{
    public interface ISystemLogRepository
    {
        Task AddAsync(SystemLog log, CancellationToken cancellationToken);
    }
}
