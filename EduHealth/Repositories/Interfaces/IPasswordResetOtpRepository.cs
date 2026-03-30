using EduHealth.Data.Entities;

namespace EduHealth.Repositories.Interfaces
{
    public interface IPasswordResetOtpRepository
    {
        Task AddAsync(PasswordResetOtp entity, CancellationToken cancellationToken = default);
        Task InvalidateActiveOtpsAsync(int userId, CancellationToken cancellationToken = default);
        Task<PasswordResetOtp?> GetValidOtpAsync(int userId, string otp, CancellationToken cancellationToken = default);
        Task<PasswordResetOtp?> GetValidResetTokenAsync(int userId, string resetToken, CancellationToken cancellationToken = default);

        void Update(PasswordResetOtp entity);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}