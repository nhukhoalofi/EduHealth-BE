using EduHealth.DTOs.Auth;

namespace EduHealth.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
        Task<MeResponseDto?> GetMeAsync(int userId, CancellationToken cancellationToken = default);
        Task<bool> LogoutAsync(CancellationToken cancellationToken = default);
        Task<bool> RequestOtpAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default);
        Task<VerifyOtpResponseDto?> VerifyOtpAsync(VerifyOtpRequestDto request, CancellationToken cancellationToken = default);
        
        // Cập nhật lại các kiểu trả về dưới đây cho khớp với AuthService:
        Task<ResetPasswordResultDto> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default);
        Task<ChangePasswordResultDto> ChangePasswordAsync(int userId, ChangePasswordRequestDto request, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message, string Field, MeResponseDto? Data)> UpdateProfileAsync(int userId, UpdateProfileRequestDto request, CancellationToken cancellationToken = default);
    }
}