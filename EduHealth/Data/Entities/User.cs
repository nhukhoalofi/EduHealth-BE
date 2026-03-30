namespace EduHealth.Data.Entities
{
    public class User
    {
        public int UserId { get; set; }
        public string Phone { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public bool IsActive { get; set; }
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string? Avatar { get; set; }
        public string? Gender { get; set; }

        // Navigation
        public ICollection<HealthVisit> HealthVisitsAsNurse { get; set; } = new List<HealthVisit>();
        public ICollection<MedicineStockLog> MedicineStockLogs { get; set; } = new List<MedicineStockLog>();
        public ICollection<PasswordResetOtp> PasswordResetOtps { get; set; } = new List<PasswordResetOtp>();
    }
}