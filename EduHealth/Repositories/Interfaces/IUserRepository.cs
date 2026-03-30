using EduHealth.Data.Entities;

namespace EduHealth.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailOrPhoneAsync(string identifier, CancellationToken cancellationToken = default);
        Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        void Update(User user);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}