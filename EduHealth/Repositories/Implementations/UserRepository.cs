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

        public async Task<Student?> GetStudentByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.Students
                .AsNoTracking()
                .Include(x => x.Class)
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            email = email.Trim();

            return await _context.Users
                .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        }

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            username = username.Trim();

            return await _context.Users
                .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);
        }

        public async Task<User?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            code = code.Trim();

            return await _context.Users
                .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
        }

        public async Task<(List<User> Items, int TotalCount)> GetPagedAsync(
            string? keyword,
            string? role,
            string? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                query = query.Where(x =>
                    x.Username.Contains(k) ||
                    x.FullName.Contains(k) ||
                    x.Email.Contains(k) ||
                    x.Phone.Contains(k));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                var r = role.Trim();
                query = query.Where(x => x.Role == r);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim();
                query = query.Where(x => x.Status == s);
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task<bool> AnyUsernameAsync(string username, int? excludeUserId = null, CancellationToken cancellationToken = default)
        {
            username = username.Trim();
            var query = _context.Users.AsQueryable();

            if (excludeUserId.HasValue)
            {
                query = query.Where(x => x.UserId != excludeUserId.Value);
            }

            return await query.AnyAsync(x => x.Username == username, cancellationToken);
        }

        public async Task<bool> AnyEmailAsync(string email, int? excludeUserId = null, CancellationToken cancellationToken = default)
        {
            email = email.Trim();
            var query = _context.Users.AsQueryable();

            if (excludeUserId.HasValue)
            {
                query = query.Where(x => x.UserId != excludeUserId.Value);
            }

            return await query.AnyAsync(x => x.Email == email, cancellationToken);
        }

        public async Task<int> GetNextUserCodeSequenceAsync(CancellationToken cancellationToken = default)
        {
            var codes = await _context.Users
                .AsNoTracking()
                .Where(x => x.Code.StartsWith("USR"))
                .Select(x => x.Code)
                .ToListAsync(cancellationToken);

            var max = 0;
            foreach (var code in codes)
            {
                if (code.Length <= 3)
                {
                    continue;
                }

                var numericPart = code[3..];
                if (int.TryParse(numericPart, out var n) && n > max)
                {
                    max = n;
                }
            }

            return max + 1;
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await _context.Users.AddAsync(user, cancellationToken);
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
