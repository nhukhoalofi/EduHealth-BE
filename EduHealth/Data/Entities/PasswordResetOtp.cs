namespace EduHealth.Data.Entities
{
    public class PasswordResetOtp
    {
        public int PasswordResetOtpId { get; set; }
        public int UserId { get; set; }
        public string OtpCode { get; set; } = null!;
        public DateTime OtpExpiresAt { get; set; }

        public bool IsVerified { get; set; }
        public DateTime? VerifiedAt { get; set; }

        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiresAt { get; set; }

        public bool IsUsed { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
    }
}