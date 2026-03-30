using EduHealth.Data.Entities;

namespace EduHealth.Repositories.Interfaces
{
    public interface IStudentRepository
    {
        Task<(List<Student> Items, int TotalCount)> GetPagedAsync(
            string? search,
            int? classId,
            bool? isActive,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<Student?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<bool> AnyEmailAsync(string email, int? excludeUserId = null, CancellationToken cancellationToken = default);
        Task<bool> AnyPhoneAsync(string phone, int? excludeUserId = null, CancellationToken cancellationToken = default);
        Task<bool> ClassExistsAsync(int classId, CancellationToken cancellationToken = default);

        Task AddAsync(User user, Student student, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IReadOnlyList<User> users, IReadOnlyList<Student> students, CancellationToken cancellationToken = default);

        void Update(Student student);
        void UpdateUser(User user);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}