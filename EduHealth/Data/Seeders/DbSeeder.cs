using EduHealth.Data.Entities;
using EduHealth.Helpers;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Data.Seeders
{
    public static class DbSeeder
    {
        public static async Task SeedAdminAsync(AppDbContext context)
        {
            await SeedUsersAsync(context);
            await SeedSchoolClassesAsync(context);
            await SeedDiseaseTypesAsync(context);
            await SeedAllergyTypesAsync(context);
            await SeedVaccinationsAsync(context);
            await SeedMedicinesAsync(context);
            await SeedStudentsAndRelationsAsync(context);
            await SeedHealthVisitsAsync(context);
            await SeedNotificationsAsync(context);
        }

        private static async Task SeedUsersAsync(AppDbContext context)
        {
            if (await context.Users.AnyAsync()) return;

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

            var nurse2 = new User
            {
                Code = "USR004",
                Username = "nurse02",
                Phone = "0900000004",
                Role = "NURSE",
                FullName = "Lê Thị Mai",
                IsActive = true,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Email = "nurse02@eduhealth.local",
                PasswordHash = PasswordHelper.HashPassword("123456"),
                Avatar = null
            };

            var studentUser2 = new User
            {
                Code = "USR005",
                Username = "HS002",
                Phone = "0900000005",
                Role = "STUDENT",
                FullName = "Ngô Thị Bích",
                IsActive = true,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Email = "hs002@eduhealth.local",
                PasswordHash = PasswordHelper.HashPassword("123456"),
                Avatar = null,
                Gender = "FEMALE"
            };

            var studentUser3 = new User
            {
                Code = "USR006",
                Username = "HS003",
                Phone = "0900000006",
                Role = "STUDENT",
                FullName = "Phạm Minh Khang",
                IsActive = true,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Email = "hs003@eduhealth.local",
                PasswordHash = PasswordHelper.HashPassword("123456"),
                Avatar = null,
                Gender = "MALE"
            };

            context.Users.AddRange(nurse2, studentUser2, studentUser3);

            await context.SaveChangesAsync();
        }

        private static async Task SeedSchoolClassesAsync(AppDbContext context)
        {
            if (await context.SchoolClasses.AnyAsync()) return;

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

            var cls3 = new SchoolClass
            {
                Code = "CLS003",
                ClassName = "5/1",
                Grade = "5",
                TeacherName = "Cô C",
                TeacherPhone = "0933333333"
            };

            context.SchoolClasses.AddRange(cls1, cls2, cls3);
            await context.SaveChangesAsync();
        }

        private static async Task SeedDiseaseTypesAsync(AppDbContext context)
        {
            if (await context.DiseaseTypes.AnyAsync()) return;

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

            var dis3 = new DiseaseType
            {
                Code = "DIS003",
                DiseaseName = "Sốt",
                Description = "Sốt nhẹ",
                StandardTreatment = "Theo dõi thân nhiệt, dùng hạ sốt khi cần"
            };

            context.DiseaseTypes.AddRange(dis1, dis2, dis3);
            await context.SaveChangesAsync();
        }

        private static async Task SeedAllergyTypesAsync(AppDbContext context)
        {
            if (await context.AllergyTypes.AnyAsync()) return;

            context.AllergyTypes.AddRange(
                new AllergyType { AllergyName = "Dị ứng hải sản", Severity = "HIGH" },
                new AllergyType { AllergyName = "Dị ứng thuốc", Severity = "MEDIUM" },
                new AllergyType { AllergyName = "Dị ứng phấn hoa", Severity = "LOW" }
            );

            await context.SaveChangesAsync();
        }

        private static async Task SeedVaccinationsAsync(AppDbContext context)
        {
            if (await context.Vaccinations.AnyAsync()) return;

            context.Vaccinations.AddRange(
                new Vaccination { Name = "COVID-19" },
                new Vaccination { Name = "Sởi" },
                new Vaccination { Name = "Uốn ván" }
            );

            await context.SaveChangesAsync();
        }

        private static async Task SeedMedicinesAsync(AppDbContext context)
        {
            if (await context.Medicines.AnyAsync()) return;

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

            var med3 = new Medicine
            {
                Code = "MED003",
                Name = "Vitamin C 100mg",
                ActiveIngredient = "Ascorbic acid",
                Unit = "VIEN",
                Packaging = "Lọ 100 viên",
                WarningThreshold = 30,
                StockQuantity = 80,
                Status = "ACTIVE",
                Note = "Bổ sung vitamin",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Medicines.AddRange(med1, med2, med3);
            await context.SaveChangesAsync();
        }

        private static async Task SeedStudentsAndRelationsAsync(AppDbContext context)
        {
            if (await context.Students.AnyAsync()) return;

            var cls1 = await context.SchoolClasses.FirstAsync(x => x.Code == "CLS001");
            var cls2 = await context.SchoolClasses.FirstAsync(x => x.Code == "CLS002");

            var u1 = await context.Users.FirstAsync(x => x.Code == "USR003");
            var u2 = await context.Users.FirstAsync(x => x.Code == "USR005");
            var u3 = await context.Users.FirstAsync(x => x.Code == "USR006");

            var s1 = new Student
            {
                UserId = u1.UserId,
                Code = "STD001",
                ClassId = cls1.ClassId,
                FullName = u1.FullName,
                DateOfBirth = new DateTime(2016, 9, 12),
                CurrentHeight = 130,
                CurrentWeight = 30.1f,
                Guardian = "Phụ huynh 1",
                Phone = u1.Phone
            };

            var s2 = new Student
            {
                UserId = u2.UserId,
                Code = "STD002",
                ClassId = cls1.ClassId,
                FullName = u2.FullName,
                DateOfBirth = new DateTime(2016, 3, 20),
                CurrentHeight = 128,
                CurrentWeight = 28.5f,
                Guardian = "Phụ huynh 2",
                Phone = u2.Phone
            };

            var s3 = new Student
            {
                UserId = u3.UserId,
                Code = "STD003",
                ClassId = cls2.ClassId,
                FullName = u3.FullName,
                DateOfBirth = new DateTime(2015, 11, 2),
                CurrentHeight = 135,
                CurrentWeight = 32.0f,
                Guardian = "Phụ huynh 3",
                Phone = u3.Phone
            };

            context.Students.AddRange(s1, s2, s3);
            await context.SaveChangesAsync();

            var allergy1 = await context.AllergyTypes.FirstAsync();
            var allergy2 = await context.AllergyTypes.Skip(1).FirstAsync();

            context.StudentAllergies.AddRange(
                new StudentAllergy { UserId = s1.UserId, AllergyId = allergy1.AllergyId, Note = "Nổi mề đay" },
                new StudentAllergy { UserId = s2.UserId, AllergyId = allergy2.AllergyId, Note = "Mẩn đỏ" }
            );

            var vac1 = await context.Vaccinations.FirstAsync();
            var vac2 = await context.Vaccinations.Skip(1).FirstAsync();

            context.StudentVaccinations.AddRange(
                new StudentVaccination { UserId = s1.UserId, VaccinationId = vac1.VaccinationId, Status = "DONE" },
                new StudentVaccination { UserId = s1.UserId, VaccinationId = vac2.VaccinationId, Status = "DONE" }
            );

            await context.SaveChangesAsync();
        }

        private static async Task SeedHealthVisitsAsync(AppDbContext context)
        {
            if (await context.HealthVisits.AnyAsync()) return;

            var nurse = await context.Users.FirstAsync(x => x.Role == "NURSE");
            var student = await context.Students.FirstAsync();
            var dis = await context.DiseaseTypes.FirstAsync();
            var med1 = await context.Medicines.FirstAsync(x => x.Code == "MED001");
            var med2 = await context.Medicines.FirstAsync(x => x.Code == "MED002");

            var visit = new HealthVisit
            {
                Code = "VIS001",
                StudentUserId = student.UserId,
                NurseId = nurse.UserId,
                VisitDate = DateTime.UtcNow.AddDays(-1),
                DiseaseId = dis.DiseaseId,
                Diagnosis = dis.DiseaseName,
                Treatment = "Theo dõi, chăm sóc tại phòng y tế",
                Note = "Dữ liệu seed",
                Symptoms = "Sốt nhẹ",
                MeasuredHeight = student.CurrentHeight,
                MeasuredWeight = student.CurrentWeight
            };

            context.HealthVisits.Add(visit);
            await context.SaveChangesAsync();

            context.VisitPrescriptions.AddRange(
                new VisitPrescription { VisitId = visit.VisitId, MedicineId = med1.MedicineId, Quantity = 2, UsageIns = "1 viên/lần, 2 lần/ngày" },
                new VisitPrescription { VisitId = visit.VisitId, MedicineId = med2.MedicineId, Quantity = 1, UsageIns = "1 gói/ngày" }
            );

            context.MedicineStockLogs.AddRange(
                new MedicineStockLog
                {
                    MedicineId = med1.MedicineId,
                    Type = "DISPENSE",
                    Quantity = 2,
                    StockBefore = med1.StockQuantity,
                    StockAfter = med1.StockQuantity - 2,
                    UserId = nurse.UserId,
                    CreatedAt = DateTime.UtcNow,
                    VisitId = visit.VisitId
                },
                new MedicineStockLog
                {
                    MedicineId = med2.MedicineId,
                    Type = "DISPENSE",
                    Quantity = 1,
                    StockBefore = med2.StockQuantity,
                    StockAfter = med2.StockQuantity - 1,
                    UserId = nurse.UserId,
                    CreatedAt = DateTime.UtcNow,
                    VisitId = visit.VisitId
                }
            );

            med1.StockQuantity -= 2;
            med2.StockQuantity -= 1;

            await context.SaveChangesAsync();
        }

        private static async Task SeedNotificationsAsync(AppDbContext context)
        {
            if (await context.Notifications.AnyAsync()) return;

            var admin = await context.Users.FirstAsync(x => x.Role == "ADMIN");
            var nurse = await context.Users.FirstAsync(x => x.Role == "NURSE");
            var studentUser = await context.Users.FirstAsync(x => x.Role == "STUDENT");

            var noti = new Notification
            {
                Title = "Thông báo kiểm tra sức khỏe",
                Content = "Nhà trường tổ chức kiểm tra sức khỏe định kỳ.",
                Type = "GENERAL",
                CreatedByUserId = admin.UserId,
                CreatedAt = DateTime.UtcNow
            };

            context.Notifications.Add(noti);
            await context.SaveChangesAsync();

            context.NotificationRecipients.AddRange(
                new NotificationRecipient { NotificationId = noti.NotificationId, UserId = nurse.UserId, IsRead = false, SentAt = DateTime.UtcNow, Status = "SENT" },
                new NotificationRecipient { NotificationId = noti.NotificationId, UserId = studentUser.UserId, IsRead = false, SentAt = DateTime.UtcNow, Status = "SENT" }
            );

            await context.SaveChangesAsync();
        }
    }
}