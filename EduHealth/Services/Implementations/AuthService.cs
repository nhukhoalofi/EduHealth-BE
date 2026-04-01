using EduHealth.Data.Entities;
using EduHealth.DTOs.Auth;
using EduHealth.Helpers;
using EduHealth.Repositories.Interfaces;
using EduHealth.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EduHealth.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordResetOtpRepository _passwordResetOtpRepository;
        private readonly JwtHelper _jwtHelper;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IPasswordResetOtpRepository passwordResetOtpRepository,
            JwtHelper jwtHelper,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _passwordResetOtpRepository = passwordResetOtpRepository;
            _jwtHelper = jwtHelper;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.Password))
            {
                return null;
            }

            var user = await _userRepository.GetByEmailOrPhoneAsync(request.Identifier, cancellationToken);

            if (user is null || !user.IsActive || string.Equals(user.Status, "LOCKED", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var isValidPassword = PasswordHelper.VerifyPassword(request.Password, user.PasswordHash);

            if (!isValidPassword)
            {
                return null;
            }

            var (token, expiresAt) = _jwtHelper.GenerateToken(user);

            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync(cancellationToken);

            return new LoginResponseDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                Avatar = user.Avatar,
                AccessToken = token,
                ExpiresAt = expiresAt
            };
        }

        public async Task<MeResponseDto?> GetMeAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

            if (user is null)
            {
                return null;
            }

            return new MeResponseDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                IsActive = user.IsActive,
                Avatar = user.Avatar
            };
        }

        public Task<bool> LogoutAsync(CancellationToken cancellationToken = default)
        {
            // Hiện tại dùng JWT access token stateless, chưa có refresh token.
            // Logout thực tế là FE xóa token phía client.
            return Task.FromResult(true);
        }

        public async Task RequestOtpAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return;
            }

            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

            // Không lộ việc email có tồn tại hay không
            if (user is null || !user.IsActive)
            {
                return;
            }

            var otpExpireMinutes = int.Parse(_configuration["Auth:OtpExpireMinutes"] ?? "5");
            var otp = OtpHelper.GenerateOtp(6);

            await _passwordResetOtpRepository.InvalidateActiveOtpsAsync(user.UserId, cancellationToken);

            var entity = new PasswordResetOtp
            {
                UserId = user.UserId,
                OtpCode = otp,
                OtpExpiresAt = DateTime.UtcNow.AddMinutes(otpExpireMinutes),
                IsVerified = false,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            await _passwordResetOtpRepository.AddAsync(entity, cancellationToken);
            await _passwordResetOtpRepository.SaveChangesAsync(cancellationToken);

            // Dev only: log OTP ra console/log
            _logger.LogInformation("Password reset OTP for {Email}: {Otp}", user.Email, otp);
        }

        public async Task<VerifyOtpResponseDto?> VerifyOtpAsync(VerifyOtpRequestDto request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Otp))
            {
                return null;
            }

            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (user is null || !user.IsActive)
            {
                return null;
            }

            var record = await _passwordResetOtpRepository.GetValidOtpAsync(user.UserId, request.Otp, cancellationToken);

            if (record is null)
            {
                return null;
            }

            var resetTokenExpireMinutes = int.Parse(_configuration["Auth:ResetTokenExpireMinutes"] ?? "10");
            var resetToken = Guid.NewGuid().ToString("N");

            record.IsVerified = true;
            record.VerifiedAt = DateTime.UtcNow;
            record.ResetToken = resetToken;
            record.ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(resetTokenExpireMinutes);

            _passwordResetOtpRepository.Update(record);
            await _passwordResetOtpRepository.SaveChangesAsync(cancellationToken);

            return new VerifyOtpResponseDto
            {
                ResetToken = resetToken,
                ExpiresAt = record.ResetTokenExpiresAt!.Value
            };
        }

        public async Task<ResetPasswordResultDto> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.ResetToken) ||
                string.IsNullOrWhiteSpace(request.NewPassword) ||
                string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                return new ResetPasswordResultDto { Success = false, Message = "Dữ liệu không hợp lệ." };
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return new ResetPasswordResultDto
                {
                    Success = false,
                    Field = "confirmPassword",
                    Message = "Xác nhận mật khẩu không khớp."
                };
            }

            if (request.NewPassword.Length < 8)
            {
                return new ResetPasswordResultDto
                {
                    Success = false,
                    Field = "newPassword",
                    Message = "Mật khẩu mới phải có ít nhất 8 ký tự."
                };
            }

            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (user is null || !user.IsActive)
            {
                return new ResetPasswordResultDto
                {
                    Success = false,
                    Field = "resetToken",
                    Message = "Reset token không hợp lệ hoặc đã hết hạn."
                };
            }

            var record = await _passwordResetOtpRepository.GetValidResetTokenAsync(user.UserId, request.ResetToken, cancellationToken);

            if (record is null)
            {
                return new ResetPasswordResultDto
                {
                    Success = false,
                    Field = "resetToken",
                    Message = "Reset token không hợp lệ hoặc đã hết hạn."
                };
            }

            user.PasswordHash = PasswordHelper.HashPassword(request.NewPassword);
            record.IsUsed = true;

            _userRepository.Update(user);
            _passwordResetOtpRepository.Update(record);

            await _userRepository.SaveChangesAsync(cancellationToken);

            return new ResetPasswordResultDto { Success = true, Message = "Đặt lại mật khẩu thành công." };
        }

        public async Task<ChangePasswordResultDto> ChangePasswordAsync(
    int userId,
    ChangePasswordRequestDto request,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.OldPassword))
            {
                return new ChangePasswordResultDto
                {
                    Success = false,
                    Field = "oldPassword",
                    Message = "Mật khẩu cũ không được để trống."
                };
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return new ChangePasswordResultDto
                {
                    Success = false,
                    Field = "newPassword",
                    Message = "Mật khẩu mới không được để trống."
                };
            }

            if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                return new ChangePasswordResultDto
                {
                    Success = false,
                    Field = "confirmPassword",
                    Message = "Xác nhận mật khẩu không được để trống."
                };
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return new ChangePasswordResultDto
                {
                    Success = false,
                    Field = "confirmPassword",
                    Message = "Xác nhận mật khẩu không khớp."
                };
            }

            if (request.NewPassword.Length < 8)
            {
                return new ChangePasswordResultDto
                {
                    Success = false,
                    Field = "newPassword",
                    Message = "Mật khẩu mới phải có ít nhất 8 ký tự."
                };
            }

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

            if (user is null)
            {
                return new ChangePasswordResultDto
                {
                    Success = false,
                    Message = "Không tìm thấy người dùng."
                };
            }

            if (!user.IsActive)
            {
                return new ChangePasswordResultDto
                {
                    Success = false,
                    Message = "Tài khoản đã bị khóa."
                };
            }

            var isOldPasswordCorrect = PasswordHelper.VerifyPassword(request.OldPassword, user.PasswordHash);

            if (!isOldPasswordCorrect)
            {
                return new ChangePasswordResultDto
                {
                    Success = false,
                    Field = "oldPassword",
                    Message = "Mật khẩu cũ không đúng."
                };
            }

            var isSameAsOld = PasswordHelper.VerifyPassword(request.NewPassword, user.PasswordHash);

            if (isSameAsOld)
            {
                return new ChangePasswordResultDto
                {
                    Success = false,
                    Field = "newPassword",
                    Message = "Mật khẩu mới không được trùng mật khẩu cũ."
                };
            }

            user.PasswordHash = PasswordHelper.HashPassword(request.NewPassword);

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync(cancellationToken);

            return new ChangePasswordResultDto
            {
                Success = true,
                Message = "Đổi mật khẩu thành công."
            };
        }
    }
}