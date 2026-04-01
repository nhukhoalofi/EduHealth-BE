using EduHealth.Data.Entities;

namespace EduHealth.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailOrPhoneAsync(string identifier, CancellationToken cancellationToken = default);
        Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
        Task<User?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

        Task<(List<User> Items, int TotalCount)> GetPagedAsync(
            string? keyword,
            string? role,
            string? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<bool> AnyUsernameAsync(string username, int? excludeUserId = null, CancellationToken cancellationToken = default);
        Task<bool> AnyEmailAsync(string email, int? excludeUserId = null, CancellationToken cancellationToken = default);

        Task AddAsync(User user, CancellationToken cancellationToken = default);
        void Update(User user);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}