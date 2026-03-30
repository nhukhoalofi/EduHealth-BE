using EduHealth.Data;
using EduHealth.Data.Entities;
using EduHealth.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailOrPhoneAsync(string identifier, CancellationToken cancellationToken = default)
        {
            identifier = identifier.Trim();

            return await _context.Users
                .FirstOrDefaultAsync(
                    x => x.Email == identifier || x.Phone == identifier,
                    cancellationToken
                );
        }

        public async Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            email = email.Trim();

            return await _context.Users
                .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        }

        public void Update(User user)
        {
            _context.Users.Update(user);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}