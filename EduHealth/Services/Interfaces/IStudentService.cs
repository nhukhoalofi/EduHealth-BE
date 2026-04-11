using EduHealth.DTOs.Students;

namespace EduHealth.Services.Interfaces
{
    public interface IStudentService
    {
        Task<StudentListResultDto> GetStudentsAsync(StudentListQueryDto query, CancellationToken cancellationToken = default);
        Task<StudentDetailDto?> GetStudentByIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<StudentCreateResultDto> CreateStudentAsync(StudentCreateRequestDto request, CancellationToken cancellationToken = default);
        Task<StudentOperationResultDto> UpdateStudentAsync(int userId, StudentUpdateRequestDto request, CancellationToken cancellationToken = default);
        Task<StudentOperationResultDto> DeleteStudentAsync(int userId, CancellationToken cancellationToken = default);

        Task<StudentImportResultDto> ImportStudentsAsync(StudentImportRequestDto request, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message, string? Field, string? ImageUrl)> UpdateStudentImageAsync(int studentUserId, IFormFile file, CancellationToken cancellationToken = default);
    }
}