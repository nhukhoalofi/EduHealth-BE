using EduHealth.DTOs.Users;

namespace EduHealth.Services.Interfaces
{
    public interface IUserService
    {
        Task<(IReadOnlyList<UserListItemDto> Items, int TotalItems, int TotalPages, int Page, int PageSize)> GetPagedAsync(UserListQueryDto query, CancellationToken cancellationToken = default);
        Task<UserDetailDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<(bool Success, int? StatusCode, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, UserDetailDto? Data)> CreateAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, UserDetailDto? Data)> UpdateAsync(string code, UpdateUserRequestDto request, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, object? Data)> UpdateStatusAsync(string code, UpdateUserStatusRequestDto request, int currentUserId, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, ResetPasswordResponseDto? Data)> ResetPasswordAsync(string code, ResetPasswordRequestDto request, CancellationToken cancellationToken = default);
    }
}
