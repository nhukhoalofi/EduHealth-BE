using EduHealth.DTOs.Auth;
using EduHealth.DTOs.Common;
using EduHealth.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace EduHealth.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Identifier))
            {
                return BadRequest(ApiResponse<object>.Fail("identifier không được rỗng", "identifier"));
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(ApiResponse<object>.Fail("password không được rỗng", "password"));
            }

            var result = await _authService.LoginAsync(request, cancellationToken);

            if (result is null)
            {
                return Unauthorized(ApiResponse<object>.Fail("Sai tài khoản hoặc mật khẩu.", "credentials"));
            }

            return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Đăng nhập thành công."));
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            await _authService.LogoutAsync(cancellationToken);

            return Ok(ApiResponse<object>.Ok(null, "Đăng xuất thành công."));
        }

        [HttpGet("me")]
        [Authorize(Roles = "STUDENT,ADMIN,NURSE")]
        public async Task<IActionResult> Me(CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ."));
            }

            var result = await _authService.GetMeAsync(userId, cancellationToken);

            if (result is null)
            {
                return NotFound(ApiResponse<object>.Fail("Không tìm thấy người dùng."));
            }

            return Ok(ApiResponse<MeResponseDto>.Ok(result, "Lấy thông tin người dùng thành công."));
        }

        [HttpPost("forgot-password/request-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestOtp([FromBody] ForgotPasswordRequestDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(ApiResponse<object>.Fail("Email không hợp lệ.", "email"));
            }

            var isValidEmail = Regex.IsMatch(
                request.Email,
                "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$",
                RegexOptions.CultureInvariant
            );

            if (!isValidEmail)
            {
                return BadRequest(ApiResponse<object>.Fail("Email không hợp lệ.", "email"));
            }

            var ok = await _authService.RequestOtpAsync(request, cancellationToken);

            if (!ok)
            {
                return BadRequest(ApiResponse<object>.Fail("Email không tồn tại trong hệ thống hoặc tài khoản đã bị khóa.", "email"));
            }

            return Ok(ApiResponse<object>.Ok(null, "OTP đã được gửi."));
        }

        [HttpPost("forgot-password/verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(ApiResponse<object>.Fail("Email không hợp lệ.", "email"));
            }

            if (string.IsNullOrWhiteSpace(request.Otp))
            {
                return BadRequest(ApiResponse<object>.Fail("OTP không hợp lệ hoặc đã hết hạn.", "otp"));
            }

            if (request.Otp.Trim().Length != 6)
            {
                return BadRequest(ApiResponse<object>.Fail("OTP không hợp lệ hoặc đã hết hạn.", "otp"));
            }

            var result = await _authService.VerifyOtpAsync(request, cancellationToken);

            if (result is null)
            {
                return BadRequest(ApiResponse<object>.Fail("OTP không hợp lệ hoặc đã hết hạn.", "otp"));
            }

            return Ok(ApiResponse<VerifyOtpResponseDto>.Ok(result, "Xác minh OTP thành công."));
        }

        [HttpPost("forgot-password/reset")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _authService.ResetPasswordAsync(request, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.Fail(result.Message, result.Field));
            }

            return Ok(ApiResponse<object>.Ok(null, result.Message));
        }

        [HttpPost("change-password")]
        [Authorize(Roles = "STUDENT,ADMIN,NURSE")]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordRequestDto request,
            CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ."));
            }

            var result = await _authService.ChangePasswordAsync(userId, request, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.Fail(result.Message, result.Field));
            }

            return Ok(ApiResponse<object>.Ok(null, result.Message));
        }

        [HttpPatch("me")]
        [Authorize(Roles = "STUDENT,ADMIN,NURSE")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequestDto request, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ."));
            }

            var result = await _authService.UpdateProfileAsync(userId, request, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.Fail(result.Message, result.Field));
            }

            return Ok(ApiResponse<MeResponseDto>.Ok(result.Data, result.Message));
        }

        [HttpPatch("me/avatar")]
        [Authorize(Roles = "STUDENT,ADMIN,NURSE")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateMyAvatar([FromForm] UpdateAvatarRequestDto request, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("Token không hợp lệ."));
            }

            var result = await _authService.UpdateAvatarAsync(userId, request.File, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.Fail(result.Message, result.Field));
            }

            return Ok(ApiResponse<object>.Ok(new { avatarUrl = result.AvatarUrl }, result.Message));
        }
    }
}