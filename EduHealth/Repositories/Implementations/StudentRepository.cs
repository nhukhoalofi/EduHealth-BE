using EduHealth.Data;
using EduHealth.Data.Entities;
using EduHealth.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Repositories.Implementations
{
    public class StudentRepository : IStudentRepository
    {
        private readonly AppDbContext _context;

        public StudentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(List<Student> Items, int TotalCount)> GetPagedAsync(
            string? search,
            int? classId,
            bool? isActive,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Students
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Class)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();

                query = query.Where(x =>
                    x.FullName.Contains(keyword) ||
                    x.User.Email.Contains(keyword) ||
                    x.User.Phone.Contains(keyword) ||
                    x.Class.ClassName.Contains(keyword));
            }

            if (classId.HasValue)
            {
                query = query.Where(x => x.ClassId == classId.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.User.IsActive == isActive.Value);
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(x => x.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task<Student?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.Students
                .Include(x => x.User)
                .Include(x => x.Class)
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        }

        public async Task<bool> AnyEmailAsync(string email, int? excludeUserId = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Users.AsQueryable();

            if (excludeUserId.HasValue)
            {
                query = query.Where(x => x.UserId != excludeUserId.Value);
            }

            return await query.AnyAsync(x => x.Email == email, cancellationToken);
        }

        public async Task<bool> AnyPhoneAsync(string phone, int? excludeUserId = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Users.AsQueryable();

            if (excludeUserId.HasValue)
            {
                query = query.Where(x => x.UserId != excludeUserId.Value);
            }

            return await query.AnyAsync(x => x.Phone == phone, cancellationToken);
        }

        public async Task<bool> ClassExistsAsync(int classId, CancellationToken cancellationToken = default)
        {
            return await _context.SchoolClasses.AnyAsync(x => x.ClassId == classId, cancellationToken);
        }

        public async Task AddAsync(User user, Student student, CancellationToken cancellationToken = default)
        {
            await _context.Users.AddAsync(user, cancellationToken);
            await _context.Students.AddAsync(student, cancellationToken);
        }

        public async Task AddRangeAsync(IReadOnlyList<User> users, IReadOnlyList<Student> students, CancellationToken cancellationToken = default)
        {
            await _context.Users.AddRangeAsync(users, cancellationToken);
            await _context.Students.AddRangeAsync(students, cancellationToken);
        }

        public void Update(Student student)
        {
            _context.Students.Update(student);
        }

        public void UpdateUser(User user)
        {
            _context.Users.Update(user);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}