using EduHealth.Data.Entities;
using EduHealth.DTOs.Users;
using EduHealth.Helpers;
using EduHealth.Repositories.Interfaces;
using EduHealth.Services.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace EduHealth.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISystemLogWriter _logWriter;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IConfiguration _configuration;

        public UserService(IUserRepository userRepository, ISystemLogWriter logWriter, ICloudinaryService cloudinaryService, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _logWriter = logWriter;
            _cloudinaryService = cloudinaryService;
            _configuration = configuration;
        }

        public async Task<(IReadOnlyList<UserListItemDto> Items, int TotalItems, int TotalPages, int Page, int PageSize)> GetPagedAsync(
            UserListQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);

            var (items, total) = await _userRepository.GetPagedAsync(query.Keyword, query.Role, query.Status, page, pageSize, cancellationToken);

            return (
                items.Select(MapListItem).ToList(),
                total,
                total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize),
                page,
                pageSize);
        }

        public async Task<UserDetailDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByCodeAsync(code, cancellationToken);
            return user is null ? null : MapDetail(user);
        }

        public async Task<(bool Success, int? StatusCode, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, UserDetailDto? Data)> CreateAsync(
            CreateUserRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var errors = ValidateCreate(request);

            if (errors.Count > 0)
            {
                return (false, 400, "Dữ liệu không hợp lệ.", errors, null);
            }

            var role = string.IsNullOrWhiteSpace(request.Role) ? "NURSE" : request.Role.Trim();
            if (!string.Equals(role, "NURSE", StringComparison.OrdinalIgnoreCase))
            {
                return (false, 400, "Dữ liệu không hợp lệ.", new[] { ("role", "INVALID_ROLE", "role chỉ nhận NURSE.") }, null);
            }

            var username = request.Username.Trim();
            var email = request.Email.Trim();

            var duplicateErrors = new List<(string Field, string Code, string Message)>();

            if (await _userRepository.AnyUsernameAsync(username, null, cancellationToken))
            {
                duplicateErrors.Add(("username", "USERNAME_ALREADY_EXISTS", "Tên đăng nhập đã tồn tại."));
            }

            if (await _userRepository.AnyEmailAsync(email, null, cancellationToken))
            {
                duplicateErrors.Add(("email", "EMAIL_ALREADY_EXISTS", "Email đã tồn tại."));
            }

            if (duplicateErrors.Count > 0)
            {
                return (false, 409, "Dữ liệu bị trùng.", duplicateErrors, null);
            }

            var now = VietnamTimeHelper.Now;
            var nextUserSeq = await _userRepository.GetNextUserCodeSequenceAsync(cancellationToken);
            var userCode = $"USR{nextUserSeq:D3}";

            var user = new User
            {
                Code = userCode,
                Username = username,
                FullName = request.FullName.Trim(),
                Email = email,
                Phone = request.PhoneNumber?.Trim() ?? string.Empty,
                Role = "NURSE",
                IsActive = true,
                Status = "ACTIVE",
                CreatedAt = now,
                UpdatedAt = now,
                PasswordHash = PasswordHelper.HashPassword(request.Password.Trim())
            };

            await _userRepository.AddAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            var saved = await _userRepository.GetByIdAsync(user.UserId, cancellationToken);

            await _logWriter.WriteAsync(new SystemLogWriteRequest
            {
                ActorUserId = null,
                Module = "USERS",
                Action = "CREATE_USER",
                TargetType = "User",
                TargetId = user.Code,
                TargetLabel = saved!.FullName,
                Description = "Tạo tài khoản y tá mới",
                Status = "SUCCESS",
                Metadata = new { }
            }, cancellationToken);

            return (true, 201, "Tạo tài khoản y tá thành công.", Array.Empty<(string, string, string)>(), MapDetail(saved!));
        }

        public async Task<(bool Success, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, UserDetailDto? Data)> UpdateAsync(
            string code,
            UpdateUserRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<(string Field, string Code, string Message)>();

            if (request.FullName is null && request.Email is null && request.PhoneNumber is null)
            {
                errors.Add(("body", "NO_FIELDS", "Ít nhất 1 field phải được gửi lên."));
                return (false, "Dữ liệu không hợp lệ.", errors, null);
            }

            var user = await _userRepository.GetByCodeAsync(code, cancellationToken);
            if (user is null)
            {
                errors.Add(("id", "USER_NOT_FOUND", "Không tồn tại user với id đã cung cấp."));
                return (false, "Không tìm thấy tài khoản.", errors, null);
            }

            if (request.FullName is not null)
            {
                if (string.IsNullOrWhiteSpace(request.FullName))
                {
                    errors.Add(("fullName", "REQUIRED", "fullName không được để trống."));
                }
                else
                {
                    user.FullName = request.FullName.Trim();
                }
            }

            if (request.Email is not null)
            {
                if (!IsValidEmail(request.Email))
                {
                    errors.Add(("email", "INVALID_EMAIL", "Email không đúng định dạng."));
                }
                else if (await _userRepository.AnyEmailAsync(request.Email.Trim(), user.UserId, cancellationToken))
                {
                    errors.Add(("email", "EMAIL_ALREADY_EXISTS", "Email đã tồn tại."));
                }
                else
                {
                    user.Email = request.Email.Trim();
                }
            }

            if (request.PhoneNumber is not null)
            {
                var phone = request.PhoneNumber.Trim();
                if (phone.Length > 0 && phone.Length > 20)
                {
                    errors.Add(("phoneNumber", "INVALID_PHONE", "Số điện thoại không hợp lệ."));
                }
                else
                {
                    user.Phone = phone;
                }
            }

            if (errors.Count > 0)
            {
                return (false, "Dữ liệu không hợp lệ.", errors, null);
            }

            user.UpdatedAt = VietnamTimeHelper.Now;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync(cancellationToken);

            await _logWriter.WriteAsync(new SystemLogWriteRequest
            {
                ActorUserId = null,
                Module = "USERS",
                Action = "UPDATE_USER",
                TargetType = "User",
                TargetId = user.Code,
                TargetLabel = user.FullName,
                Description = "Cập nhật tài khoản y tá",
                Status = "SUCCESS",
                Metadata = new { }
            }, cancellationToken);

            var saved = await _userRepository.GetByIdAsync(user.UserId, cancellationToken);
            return (true, "Cập nhật tài khoản thành công.", Array.Empty<(string, string, string)>(), MapDetail(saved!));
        }

        public async Task<(bool Success, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, object? Data)> UpdateStatusAsync(
            string code,
            UpdateUserStatusRequestDto request,
            int currentUserId,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<(string Field, string Code, string Message)>();

            if (string.IsNullOrWhiteSpace(request.Status))
            {
                errors.Add(("status", "INVALID_STATUS", "status chỉ được phép là ACTIVE hoặc LOCKED."));
                return (false, "Trạng thái không hợp lệ.", errors, null);
            }

            var status = request.Status.Trim().ToUpperInvariant();
            if (status is not ("ACTIVE" or "LOCKED"))
            {
                errors.Add(("status", "INVALID_STATUS", "status chỉ được phép là ACTIVE hoặc LOCKED."));
                return (false, "Trạng thái không hợp lệ.", errors, null);
            }

            var user = await _userRepository.GetByCodeAsync(code, cancellationToken);
            if (user is null)
            {
                errors.Add(("id", "USER_NOT_FOUND", "Không tồn tại user với id đã cung cấp."));
                return (false, "Không tìm thấy tài khoản.", errors, null);
            }

            if (user.UserId == currentUserId)
            {
                errors.Add(("id", "CANNOT_UPDATE_SELF", "Không thể tự khóa/mở chính mình."));
                return (false, "Dữ liệu không hợp lệ.", errors, null);
            }

            user.Status = status;
            user.LockReason = status == "LOCKED" ? request.Reason?.Trim() : null;
            user.IsActive = status == "ACTIVE";
            user.UpdatedAt = VietnamTimeHelper.Now;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync(cancellationToken);

            var actor = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
            await _logWriter.WriteAsync(new SystemLogWriteRequest
            {
                ActorUserId = currentUserId,
                ActorName = actor?.FullName,
                ActorUsername = actor?.Username,
                ActorRole = actor?.Role,
                Module = "USERS",
                Action = status == "LOCKED" ? "LOCK_USER" : "UNLOCK_USER",
                TargetType = "User",
                TargetId = user.Code,
                TargetLabel = user.FullName,
                Description = status == "LOCKED"
                    ? $"Khóa tài khoản {user.Username}" 
                    : $"Mở khóa tài khoản {user.Username}",
                Status = "SUCCESS",
                Metadata = new { status = user.Status, reason = user.LockReason }
            }, cancellationToken);

            return (true, "Cập nhật trạng thái tài khoản thành công.", Array.Empty<(string, string, string)>(), new
            {
                id = user.Code,
                status = user.Status,
                reason = user.LockReason,
                updatedAt = user.UpdatedAt
            });
        }

        public async Task<(bool Success, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, ResetPasswordResponseDto? Data)> ResetPasswordAsync(
            string code,
            ResetPasswordRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<(string Field, string Code, string Message)>();

            if (string.IsNullOrWhiteSpace(request.Mode))
            {
                errors.Add(("mode", "INVALID_MODE", "mode không hợp lệ."));
                return (false, "Dữ liệu không hợp lệ.", errors, null);
            }

            var mode = request.Mode.Trim().ToUpperInvariant();
            if (mode is not ("CUSTOM" or "TEMPORARY"))
            {
                errors.Add(("mode", "INVALID_MODE", "mode không hợp lệ."));
                return (false, "Dữ liệu không hợp lệ.", errors, null);
            }

            var user = await _userRepository.GetByCodeAsync(code, cancellationToken);
            if (user is null)
            {
                errors.Add(("id", "USER_NOT_FOUND", "Không tồn tại user với id đã cung cấp."));
                return (false, "Không tìm thấy tài khoản.", errors, null);
            }

            string? temp = null;
            string newPassword;

            if (mode == "CUSTOM")
            {
                if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Trim().Length < 6)
                {
                    errors.Add(("newPassword", "INVALID_PASSWORD", "Mật khẩu tối thiểu 6 ký tự."));
                    return (false, "Dữ liệu không hợp lệ.", errors, null);
                }

                newPassword = request.NewPassword.Trim();
            }
            else
            {
                temp = GenerateTemporaryPassword();
                newPassword = temp;
            }

            user.PasswordHash = PasswordHelper.HashPassword(newPassword);
            user.UpdatedAt = VietnamTimeHelper.Now;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync(cancellationToken);

            await _logWriter.WriteAsync(new SystemLogWriteRequest
            {
                ActorUserId = null,
                Module = "USERS",
                Action = "RESET_USER_PASSWORD",
                TargetType = "User",
                TargetId = user.Code,
                TargetLabel = user.FullName,
                Description = $"Reset mật khẩu tài khoản ({mode.ToLower()})",
                Status = "SUCCESS",
                Metadata = new { mode = mode }
            }, cancellationToken);

            return (true, mode == "CUSTOM" ? "Reset mật khẩu thành công." : "Reset mật khẩu tạm thành công.", Array.Empty<(string, string, string)>(), new ResetPasswordResponseDto
            {
                Id = user.Code,
                ResetMode = mode,
                TemporaryPassword = temp,
                UpdatedAt = user.UpdatedAt
            });
        }

        public async Task<(bool Success, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, object? Data)> UpdateAvatarByCodeAsync(
            string code,
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<(string Field, string Code, string Message)>();

            if (file is null || file.Length == 0)
            {
                errors.Add(("file", "REQUIRED", "Vui lòng chọn file hình ảnh."));
                return (false, "Dữ liệu không hợp lệ.", errors, null);
            }

            if (string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(("file", "INVALID_FILE", "File không đúng định dạng hình ảnh."));
                return (false, "Dữ liệu không hợp lệ.", errors, null);
            }

            var user = await _userRepository.GetByCodeAsync(code, cancellationToken);
            if (user is null)
            {
                errors.Add(("id", "USER_NOT_FOUND", "Không tồn tại user với id đã cung cấp."));
                return (false, "Không tìm thấy tài khoản.", errors, null);
            }

            var folderRoot = _configuration["Cloudinary:Folder"];
            var folder = string.IsNullOrWhiteSpace(folderRoot)
                ? "eduhealth/users"
                : $"{folderRoot.Trim().TrimEnd('/')}/users";

            try
            {
                var (url, _) = await _cloudinaryService.UploadImageAsync(file, folder, cancellationToken);

                user.Avatar = url;
                user.UpdatedAt = VietnamTimeHelper.Now;
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync(cancellationToken);

                await _logWriter.WriteAsync(new SystemLogWriteRequest
                {
                    ActorUserId = null,
                    ActorName = "Hệ thống",
                    ActorRole = "SYSTEM",
                    Module = "USERS",
                    Action = "UPDATE_USER_AVATAR",
                    TargetType = "User",
                    TargetId = user.Code,
                    TargetLabel = user.FullName,
                    Description = $"Cập nhật ảnh đại diện cho tài khoản {user.Username}",
                    Status = "SUCCESS",
                    Metadata = new { avatarUrl = url }
                }, cancellationToken);

                return (true, "Cập nhật ảnh đại diện thành công.", Array.Empty<(string, string, string)>(), new { avatarUrl = url });
            }
            catch
            {
                errors.Add(("file", "UPLOAD_FAILED", "Upload hình ảnh thất bại."));
                return (false, "Upload hình ảnh thất bại.", errors, null);
            }
        }

        private static UserListItemDto MapListItem(User x) => new()
        {
            Id = x.Code,
            Username = x.Username,
            FullName = x.FullName,
            Email = x.Email,
            PhoneNumber = x.Phone,
            Role = x.Role,
            Status = x.Status,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };

        private static UserDetailDto MapDetail(User x) => new()
        {
            Id = x.Code,
            Username = x.Username,
            FullName = x.FullName,
            Email = x.Email,
            PhoneNumber = x.Phone,
            Role = x.Role,
            Status = x.Status,
            LastLoginAt = x.LastLoginAt,
            LockReason = x.LockReason,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };

        private static List<(string Field, string Code, string Message)> ValidateCreate(CreateUserRequestDto request)
        {
            var errors = new List<(string Field, string Code, string Message)>();

            if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Trim().Length is < 3 or > 50)
            {
                errors.Add(("username", "INVALID_USERNAME", "username bắt buộc, 3-50 ký tự."));
            }

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Trim().Length < 6)
            {
                errors.Add(("password", "INVALID_PASSWORD", "password tối thiểu 6 ký tự."));
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                errors.Add(("fullName", "REQUIRED", "fullName không được để trống."));
            }

            if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email))
            {
                errors.Add(("email", "INVALID_EMAIL", "Email không đúng định dạng."));
            }

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && request.PhoneNumber.Trim().Length > 20)
            {
                errors.Add(("phoneNumber", "INVALID_PHONE", "Số điện thoại không hợp lệ."));
            }

            return errors;
        }

        private static bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email.Trim(), "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$", RegexOptions.CultureInvariant);
        }

        private static string GenerateTemporaryPassword()
        {
            return $"Tmp@{Random.Shared.Next(1000, 9999)}";
        }
    }
}
