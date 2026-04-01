using EduHealth.Data.Entities;
using EduHealth.Helpers;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Data.Seeders
{
    public static class DbSeeder
    {
        public static async Task SeedAdminAsync(AppDbContext context)
        {
            if (await context.Users.AnyAsync())
            {
                return;
            }

            var admin = new User
            {
                Code = "USR001",
                Username = "admin",
                Phone = "0900000001",
                Role = "ADMIN",
                FullName = "System Admin",
                IsActive = true,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Email = "admin@eduhealth.local",
                PasswordHash = PasswordHelper.HashPassword("123456Aa@"),
                Avatar = null
            };

            context.Users.Add(admin);

            var nurse = new User
            {
                Code = "USR002",
                Username = "nurse01",
                Phone = "0900000002",
                Role = "NURSE",
                FullName = "Nguyễn Thị Lan",
                IsActive = true,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Email = "nurse01@eduhealth.local",
                PasswordHash = PasswordHelper.HashPassword("123456"),
                Avatar = null
            };

            var studentUser = new User
            {
                Code = "USR003",
                Username = "HS001",
                Phone = "0900000003",
                Role = "STUDENT",
                FullName = "Trần Văn An",
                IsActive = true,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Email = "hs001@eduhealth.local",
                PasswordHash = PasswordHelper.HashPassword("123456"),
                Avatar = null,
                Gender = "MALE"
            };

            context.Users.AddRange(nurse, studentUser);

            var cls1 = new SchoolClass
            {
                Code = "CLS001",
                ClassName = "4/2",
                Grade = "4",
                TeacherName = "Cô A",
                TeacherPhone = "0911111111"
            };

            var cls2 = new SchoolClass
            {
                Code = "CLS002",
                ClassName = "4/3",
                Grade = "4",
                TeacherName = "Cô B",
                TeacherPhone = "0922222222"
            };

            context.SchoolClasses.AddRange(cls1, cls2);

            var dis1 = new DiseaseType
            {
                Code = "DIS001",
                DiseaseName = "Cảm cúm",
                Description = "Triệu chứng cảm cúm",
                StandardTreatment = "Nghỉ ngơi, uống nhiều nước"
            };

            var dis2 = new DiseaseType
            {
                Code = "DIS002",
                DiseaseName = "Đau bụng",
                Description = "Đau bụng nhẹ",
                StandardTreatment = "Theo dõi, bù nước"
            };

            context.DiseaseTypes.AddRange(dis1, dis2);

            var med1 = new Medicine
            {
                Code = "MED001",
                Name = "Paracetamol 500mg",
                ActiveIngredient = "Paracetamol",
                Unit = "VIEN",
                Packaging = "Hộp 10 vỉ",
                WarningThreshold = 50,
                StockQuantity = 200,
                Status = "ACTIVE",
                Note = "Dùng hạ sốt, giảm đau",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var med2 = new Medicine
            {
                Code = "MED002",
                Name = "ORS",
                ActiveIngredient = "Oresol",
                Unit = "GOI",
                Packaging = "Hộp 24 gói",
                WarningThreshold = 20,
                StockQuantity = 100,
                Status = "ACTIVE",
                Note = "Bù nước",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Medicines.AddRange(med1, med2);

            await context.SaveChangesAsync();

            var student = new Student
            {
                UserId = studentUser.UserId,
                Code = "STD001",
                ClassId = cls1.ClassId,
                FullName = studentUser.FullName,
                DateOfBirth = new DateTime(2016, 9, 12),
                CurrentHeight = 130,
                CurrentWeight = 30.1f,
                Guardian = "Phụ huynh",
                Phone = studentUser.Phone
            };

            context.Students.Add(student);
            await context.SaveChangesAsync();
        }
    }
}