using EduHealth.Data;
using EduHealth.Data.Entities;
using EduHealth.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Repositories.Implementations
{
    public class PasswordResetOtpRepository : IPasswordResetOtpRepository
    {
        private readonly AppDbContext _context;

        public PasswordResetOtpRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(PasswordResetOtp entity, CancellationToken cancellationToken = default)
        {
            await _context.PasswordResetOtps.AddAsync(entity, cancellationToken);
        }

        public async Task InvalidateActiveOtpsAsync(int userId, CancellationToken cancellationToken = default)
        {
            var records = await _context.PasswordResetOtps
                .Where(x => x.UserId == userId && !x.IsUsed && !x.IsVerified)
                .ToListAsync(cancellationToken);

            foreach (var item in records)
            {
                item.OtpExpiresAt = DateTime.UtcNow;
            }
        }

        public async Task<PasswordResetOtp?> GetValidOtpAsync(int userId, string otp, CancellationToken cancellationToken = default)
        {
            return await _context.PasswordResetOtps
                .Where(x =>
                    x.UserId == userId &&
                    x.OtpCode == otp &&
                    !x.IsUsed &&
                    !x.IsVerified &&
                    x.OtpExpiresAt >= DateTime.UtcNow)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<PasswordResetOtp?> GetValidResetTokenAsync(int userId, string resetToken, CancellationToken cancellationToken = default)
        {
            return await _context.PasswordResetOtps
                .Where(x =>
                    x.UserId == userId &&
                    x.ResetToken == resetToken &&
                    x.IsVerified &&
                    !x.IsUsed &&
                    x.ResetTokenExpiresAt != null &&
                    x.ResetTokenExpiresAt >= DateTime.UtcNow)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public void Update(PasswordResetOtp entity)
        {
            _context.PasswordResetOtps.Update(entity);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}