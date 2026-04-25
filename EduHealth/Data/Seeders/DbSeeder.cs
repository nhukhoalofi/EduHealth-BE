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
            await SeedVaccinationCampaignsAsync(context);
            await SeedBulkDataAsync(context);
        }

        private static async Task SeedBulkDataAsync(AppDbContext context)
        {
            await CleanupBulkSeedDataAsync(context);
            await SeedMoreClassesAsync(context);
            await SeedMoreMedicinesAsync(context);
            await SeedManyStudentsAsync(context);
        }

        private static async Task CleanupBulkSeedDataAsync(AppDbContext context)
        {
            // Remove previously seeded "bulk" data so DB looks realistic.
            // This targets only records created by this seeder range (CLS101-103, USR/HS >= 101).
            const int userStart = 100;

            // 1. Clean up bulk users and all dependencies
            var bulkUsers = await context.Users
                .Where(u => u.Username.StartsWith("HS") && u.Code.StartsWith("USR") && u.UserId >= userStart)
                .ToListAsync();

            var bulkUserIds = bulkUsers.Select(u => u.UserId).ToList();

            if (bulkUserIds.Count > 0)
            {
                var visits = await context.HealthVisits
                    .Where(v => bulkUserIds.Contains(v.StudentUserId))
                    .Select(v => v.VisitId)
                    .ToListAsync();

                if (visits.Count > 0)
                {
                    context.VisitPrescriptions.RemoveRange(context.VisitPrescriptions.Where(x => visits.Contains(x.VisitId)));
                    context.MedicineStockLogs.RemoveRange(context.MedicineStockLogs.Where(x => x.VisitId.HasValue && visits.Contains(x.VisitId.Value)));
                    context.HealthVisits.RemoveRange(context.HealthVisits.Where(x => visits.Contains(x.VisitId)));
                }

                // Remove targeted student dependencies, then the students, then the users themselves
                context.StudentVaccinations.RemoveRange(context.StudentVaccinations.Where(x => bulkUserIds.Contains(x.UserId)));
                context.StudentAllergies.RemoveRange(context.StudentAllergies.Where(x => bulkUserIds.Contains(x.UserId)));
                context.Students.RemoveRange(context.Students.Where(x => bulkUserIds.Contains(x.UserId)));
                
                context.Users.RemoveRange(bulkUsers);
            }

            // 2. Remove bulk classes (CLS101..CLS110)
            var bulkClassCodes = Enumerable.Range(101, 10).Select(x => $"CLS{x:D3}").ToHashSet();
            var classIds = await context.SchoolClasses
                .Where(x => bulkClassCodes.Contains(x.Code))
                .Select(x => x.ClassId)
                .ToListAsync();

            if (classIds.Count > 0)
            {
                context.SchoolClasses.RemoveRange(context.SchoolClasses.Where(x => classIds.Contains(x.ClassId)));
            }

            // 3. Remove bulk medicines MED101..MED130
            context.Medicines.RemoveRange(context.Medicines.Where(x => x.Code.CompareTo("MED101") >= 0 && x.Code.CompareTo("MED130") <= 0));

            await context.SaveChangesAsync();
        }

        private static async Task SeedMoreClassesAsync(AppDbContext context)
        {
            // Seed 5 grades, each grade has 2 classes: 1/1, 1/2 ... 5/1, 5/2
            var existing = await context.SchoolClasses
                .Where(x => x.Code.StartsWith("CLS"))
                .Select(x => x.Code)
                .ToHashSetAsync();

            var teacherLastNames = new[] { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Vũ", "Đỗ", "Bùi" };
            var teacherMiddleNames = new[] { "Thị", "Văn", "Ngọc", "Thu", "Minh", "Quỳnh" };
            var teacherFirstNames = new[] { "Lan", "Mai", "Hương", "Linh", "Hạnh", "Trang", "Nhung", "Phong" };

            var random = new Random(2026);
            var need = new List<SchoolClass>();
            var codeNumber = 101;

            for (var grade = 1; grade <= 5; grade++)
            {
                for (var section = 1; section <= 2; section++)
                {
                    var code = $"CLS{codeNumber:D3}";
                    codeNumber++;

                    if (existing.Contains(code)) continue;

                    var teacherName = $"{teacherLastNames[random.Next(teacherLastNames.Length)]} " +
                                      $"{teacherMiddleNames[random.Next(teacherMiddleNames.Length)]} " +
                                      $"{teacherFirstNames[random.Next(teacherFirstNames.Length)]}";

                    need.Add(new SchoolClass
                    {
                        Code = code,
                        ClassName = $"{grade}/{section}",
                        Grade = grade.ToString(),
                        TeacherName = teacherName,
                        TeacherPhone = $"09{random.Next(10000000, 99999999)}"
                    });
                }
            }

            if (need.Count > 0)
            {
                context.SchoolClasses.AddRange(need);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedMoreMedicinesAsync(AppDbContext context)
        {
            // seed 30 medicines with code MEDxxx (continue from 100+)
            var existingCodes = await context.Medicines
                .Where(x => x.Code.StartsWith("MED"))
                .Select(x => x.Code)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var meds = new List<Medicine>();

            for (var i = 1; i <= 30; i++)
            {
                var code = $"MED{(100 + i):D3}";
                if (existingCodes.Contains(code)) continue;

                var (name, ing, unit, pack, note) = i switch
                {
                    1 => ("Ibuprofen 200mg", "Ibuprofen", "VIEN", "Hộp 10 vỉ", "Giảm đau, hạ sốt"),
                    2 => ("Cetirizine 10mg", "Cetirizine", "VIEN", "Hộp", "Chống dị ứng"),
                    3 => ("Amoxicillin 500mg", "Amoxicillin", "VIEN", "Hộp", "Kháng sinh"),
                    4 => ("Natri clorid 0.9%", "NaCl", "CHAI", "Chai 500ml", "Vệ sinh mũi"),
                    _ => ($"Thuốc bổ sung {i}", $"Hoạt chất {i}", i % 3 == 0 ? "GOI" : "VIEN", i % 2 == 0 ? "Hộp" : "Lọ", "Dữ liệu seed")
                };

                meds.Add(new Medicine
                {
                    Code = code,
                    Name = name,
                    ActiveIngredient = ing,
                    Unit = unit,
                    Packaging = pack,
                    WarningThreshold = 20,
                    StockQuantity = 500,
                    Status = "ACTIVE",
                    Note = note,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            if (meds.Count > 0)
            {
                context.Medicines.AddRange(meds);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedManyStudentsAsync(AppDbContext context)
        {
            // Create 300 students/users: 5 grades * 2 classes/grade * 30 students/class
            const int studentsPerClass = 30;

            var bulkClasses = await context.SchoolClasses
                .Where(x => x.Code.CompareTo("CLS101") >= 0 && x.Code.CompareTo("CLS110") <= 0)
                .OrderBy(x => x.Code)
                .ToListAsync();

            if (bulkClasses.Count == 0)
            {
                return;
            }

            var total = bulkClasses.Count * studentsPerClass;

            const int userStart = 100;

            var existingSeqs = await context.Users
                .AsNoTracking()
                .Where(x => x.Code.StartsWith("USR") && x.Username.StartsWith("HS"))
                .Select(x => x.Code)
                .ToListAsync();

            var maxSeq = existingSeqs
                .Select(code =>
                {
                    if (code.Length >= 6 && int.TryParse(code[3..], out var n)) return n;
                    return 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            var startSeq = Math.Max(userStart + 1, maxSeq + 1);
            var endSeq = userStart + total;

            if (startSeq > endSeq) return;

            var now = DateTime.UtcNow;
            var plannedCount = endSeq - startSeq + 1;
            var users = new List<User>(capacity: plannedCount);
            var students = new List<Student>(capacity: plannedCount);

            var lastNames = new[]
            {
                "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Võ", "Đặng", "Bùi", "Đỗ", "Hồ",
                "Ngô", "Dương", "Lý", "Đinh", "Mai", "Trương", "Phan", "Tạ", "Vũ", "Huỳnh"
            };
            var middleNames = new[]
            {
                "Văn", "Thị", "Minh", "Hồng", "Đức", "Quang", "Thu", "Ngọc", "Gia", "Khánh",
                "Thanh", "Phương", "Hữu", "Hoài", "Anh", "Bảo", "Cẩm", "Nhật", "Tuấn", "Thảo"
            };
            var firstNames = new[]
            {
                "An", "Bình", "Chi", "Dũng", "Hà", "Huy", "Khánh", "Lan", "Mai", "Nam",
                "Oanh", "Phúc", "Quân", "Trang", "Vy", "Bảo", "Giang", "Hiếu", "Khoa", "Lâm",
                "Linh", "Long", "My", "Nhi", "Nhung", "Phát", "Phương", "Quyên", "Sơn", "Thảo",
                "Thịnh", "Thư", "Tiến", "Trúc", "Tuấn", "Uyên", "Yến", "Đạt", "Đăng", "Hân"
            };
            var guardianLastNames = new[] { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Đặng" };
            var guardianFirstNames = new[] { "Hùng", "Hòa", "Hải", "Hạnh", "Hoa", "Thủy", "Tuấn", "Tâm", "Hiền", "Duyên" };
            var random = new Random(2026);
            var usedFullNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Generate stable unique phone/email per index (email uses @gmail.com per requirement)
            for (var seq = startSeq; seq <= endSeq; seq++)
            {
                var userCode = $"USR{seq:D3}";
                var username = $"HS{seq:D3}";
                var email = $"hs{seq:D3}@gmail.com";
                var phone = $"098{seq:D7}"; // 10 digits

                string fullName;
                var tryCount = 0;
                do
                {
                    fullName = $"{lastNames[random.Next(lastNames.Length)]} {middleNames[random.Next(middleNames.Length)]} {firstNames[random.Next(firstNames.Length)]}";
                    tryCount++;

                    if (tryCount > 8)
                    {
                        fullName = $"{fullName} {seq % 10}";
                    }
                } while (!usedFullNames.Add(fullName));

                var user = new User
                {
                    Code = userCode,
                    Username = username,
                    Phone = phone,
                    Role = "STUDENT",
                    FullName = fullName,
                    IsActive = true,
                    Status = "ACTIVE",
                    CreatedAt = now,
                    UpdatedAt = now,
                    Email = email,
                    PasswordHash = PasswordHelper.HashPassword("123456"),
                    Avatar = null,
                    Gender = seq % 2 == 0 ? "FEMALE" : "MALE"
                };
                users.Add(user);
            }

            // Insert users first to get UserId, then students (Student PK is UserId)
            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            // Reload inserted users to get IDs in a single query (avoid tracking issues)
            var insertedUserIds = await context.Users.AsNoTracking()
                .Where(x => x.Code.StartsWith("USR") && x.Username.StartsWith("HS") && x.UserId >= userStart)
                .OrderBy(x => x.UserId)
                .Select(x => new { x.UserId, x.Code, x.Username, x.FullName, x.Phone })
                .ToListAsync();

            // Only create students for users that don't have student rows yet
            var existingStudentUserIds = await context.Students
                .AsNoTracking()
                .Where(x => x.Code.StartsWith("STD") && x.UserId >= userStart)
                .Select(x => x.UserId)
                .ToHashSetAsync();

            foreach (var (u, idx) in insertedUserIds.Select((u, idx) => (u, idx)))
            {
                if (existingStudentUserIds.Contains(u.UserId)) continue;

                // 30 students per class
                var classIndex = (idx / studentsPerClass) % bulkClasses.Count;
                var assignedClass = bulkClasses[classIndex];
                var classId = assignedClass.ClassId;
                var grade = int.TryParse(assignedClass.Grade, out var g) ? g : 1;
                var guardian = $"{guardianLastNames[u.UserId % guardianLastNames.Length]} {guardianFirstNames[u.UserId % guardianFirstNames.Length]}";

                var birthYear = 2021 - grade;
                var dayOfYear = random.Next(1, 365);
                var dob = new DateTime(birthYear, 1, 1).AddDays(dayOfYear - 1);

                var height = (float)(105 + grade * 6 + random.NextDouble() * 12);
                var weight = (float)(16 + grade * 3 + random.NextDouble() * 10);

                students.Add(new Student
                {
                    UserId = u.UserId,
                    Code = $"STD{u.UserId:D3}",
                    ClassId = classId,
                    FullName = u.FullName,
                    DateOfBirth = dob,
                    CurrentHeight = height,
                    CurrentWeight = weight,
                    MedicalHistoryNotes = null,
                    Guardian = guardian,
                    Phone = u.Phone
                });
            }

            if (students.Count > 0)
            {
                context.Students.AddRange(students);
                await context.SaveChangesAsync();
            }

            // Seed health visits for bulk students (1 visit/student) for testing.
            var nurse = await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Role == "NURSE");
            var diseases = await context.DiseaseTypes.AsNoTracking().OrderBy(x => x.DiseaseId).ToListAsync();
            var med1 = await context.Medicines.AsNoTracking().OrderBy(x => x.MedicineId).FirstOrDefaultAsync();
            var med2 = await context.Medicines.AsNoTracking().OrderByDescending(x => x.MedicineId).FirstOrDefaultAsync();

            if (nurse == null || !diseases.Any() || med1 == null || med2 == null) return;

            var bulkStudentIds = await context.Students
                .AsNoTracking()
                .Where(x => x.Code.StartsWith("STD") && x.UserId >= userStart)
                .Select(x => x.UserId)
                .ToListAsync();

            var existingVisitStudentIds = await context.HealthVisits
                .AsNoTracking()
                .Where(x => x.Code.StartsWith("VIS") && x.StudentUserId >= userStart)
                .Select(x => x.StudentUserId)
                .ToHashSetAsync();

            var newVisits = new List<HealthVisit>();
            foreach (var sid in bulkStudentIds)
            {
                if (existingVisitStudentIds.Contains(sid)) continue;

                newVisits.Add(new HealthVisit
                {
                    Code = $"VIS{sid:D3}",
                    StudentUserId = sid,
                    NurseId = nurse.UserId,
                    VisitDate = DateTime.UtcNow.AddDays(-(sid % 30)),
                    Symptoms = (sid % 3) switch
                    {
                        0 => "Sốt nhẹ, mệt mỏi",
                        1 => "Đau bụng, buồn nôn",
                        _ => "Ho, sổ mũi"
                    },
                    DiseaseId = diseases[sid % diseases.Count].DiseaseId,
                    Diagnosis = diseases[sid % diseases.Count].DiseaseName,
                    Treatment = (sid % 3) switch
                    {
                        0 => "Theo dõi thân nhiệt, nghỉ ngơi",
                        1 => "Bù nước, theo dõi triệu chứng",
                        _ => "Nghỉ ngơi, uống nhiều nước ấm"
                    },
                    Note = "Khám tại phòng y tế trường",
                    MeasuredHeight = 120 + (sid % 30),
                    MeasuredWeight = 20 + (sid % 15)
                });
            }

            if (newVisits.Count == 0) return;

            context.HealthVisits.AddRange(newVisits);
            await context.SaveChangesAsync();

            var visitIds = await context.HealthVisits
                .AsNoTracking()
                .Where(x => x.Code.StartsWith("VIS") && x.StudentUserId >= userStart)
                .Select(x => new { x.VisitId, x.StudentUserId })
                .ToListAsync();

            var newPrescriptions = new List<VisitPrescription>();
            var newLogs = new List<MedicineStockLog>();
            foreach (var v in visitIds)
            {
                // 2 items / visit
                newPrescriptions.Add(new VisitPrescription { VisitId = v.VisitId, MedicineId = med1.MedicineId, Quantity = 1, UsageIns = "1 viên sau ăn" });
                newPrescriptions.Add(new VisitPrescription { VisitId = v.VisitId, MedicineId = med2.MedicineId, Quantity = 1, UsageIns = "1 viên trước ngủ" });

                // stock logs (do not force stock quantity consistency too strictly for seed)
                newLogs.Add(new MedicineStockLog
                {
                    MedicineId = med1.MedicineId,
                    UserId = nurse.UserId,
                    Quantity = 1,
                    StockBefore = med1.StockQuantity,
                    StockAfter = Math.Max(0, med1.StockQuantity - 1),
                    Reason = "Seed dispense",
                    CreatedAt = DateTime.UtcNow,
                    Type = "DISPENSE",
                    VisitId = v.VisitId,
                    Note = "Seed"
                });
            }

            context.VisitPrescriptions.AddRange(newPrescriptions);
            context.MedicineStockLogs.AddRange(newLogs);
            await context.SaveChangesAsync();
        }

        private static async Task SeedVaccinationCampaignsAsync(AppDbContext context)
        {
            var admin = await context.Users.FirstOrDefaultAsync(x => x.Role == "ADMIN");
            var vaccinations = await context.Vaccinations.OrderBy(x => x.VaccinationId).Take(2).ToListAsync();
            var classes = await context.SchoolClasses
                .Where(x => x.Code.CompareTo("CLS101") >= 0 && x.Code.CompareTo("CLS110") <= 0)
                .OrderBy(x => x.ClassId)
                .ToListAsync();
            var students = await context.Students
                .AsNoTracking()
                .Where(x => x.UserId >= 100 && x.Code.StartsWith("STD"))
                .Select(x => new { x.UserId, x.ClassId })
                .ToListAsync();

            if (admin == null || !vaccinations.Any() || !classes.Any() || !students.Any()) return;

            var random = new Random(2027);
            var campaignSpecs = new[]
            {
                new
                {
                    Code = "VACB001",
                    Name = "Chiến dịch tiêm chủng học sinh - Mũi 1",
                    Dose = 1,
                    ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                    Vaccine = vaccinations[0]
                },
                new
                {
                    Code = "VACB002",
                    Name = "Chiến dịch tiêm chủng học sinh - Mũi 2",
                    Dose = 2,
                    ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45)),
                    Vaccine = vaccinations.Count > 1 ? vaccinations[1] : vaccinations[0]
                }
            };

            foreach (var spec in campaignSpecs)
            {
                var camp = await context.VaccinationCampaigns.FirstOrDefaultAsync(x => x.Code == spec.Code);
                if (camp == null)
                {
                    camp = new VaccinationCampaign
                    {
                        Code = spec.Code,
                        Name = spec.Name,
                        VaccineName = spec.Vaccine.Name,
                        DoseNumber = spec.Dose,
                        ScheduledDate = spec.ScheduledDate,
                        TargetType = "CLASS",
                        Status = "ACTIVE",
                        Note = "Seed data for 300 students",
                        CreatedByUserId = admin.UserId,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.VaccinationCampaigns.Add(camp);
                    await context.SaveChangesAsync();
                }

                var existingTargetClassIds = await context.VaccinationCampaignTargetClasses
                    .AsNoTracking()
                    .Where(x => x.CampaignId == camp.CampaignId)
                    .Select(x => x.ClassId)
                    .ToHashSetAsync();

                var missingTargets = classes
                    .Where(c => !existingTargetClassIds.Contains(c.ClassId))
                    .Select(c => new VaccinationCampaignTargetClass
                    {
                        CampaignId = camp.CampaignId,
                        ClassId = c.ClassId
                    })
                    .ToList();

                if (missingTargets.Count > 0)
                {
                    context.VaccinationCampaignTargetClasses.AddRange(missingTargets);
                    await context.SaveChangesAsync();
                }

                var existingVaccineUserIds = await context.StudentVaccinations
                    .AsNoTracking()
                    .Where(x => x.CampaignId == camp.CampaignId && x.UserId >= 100)
                    .Select(x => x.UserId)
                    .ToHashSetAsync();

                var records = new List<StudentVaccination>();
                foreach (var s in students)
                {
                    if (existingVaccineUserIds.Contains(s.UserId)) continue;

                    var status = (random.Next(100)) switch
                    {
                        < 70 => "DONE",
                        < 82 => "PENDING",
                        < 90 => "POSTPONED",
                        < 96 => "ABSENT",
                        _ => "CONTRAINDICATED"
                    };

                    records.Add(new StudentVaccination
                    {
                        UserId = s.UserId,
                        CampaignId = camp.CampaignId,
                        VaccinationId = spec.Vaccine.VaccinationId,
                        Status = status,
                        VaccinatedAt = status == "DONE" ? camp.ScheduledDate : null,
                        LotNumber = status == "DONE" ? $"LOT-{spec.Code}-{(s.UserId % 35) + 1:D2}" : null,
                        Note = "Seed vaccination record",
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                if (records.Count > 0)
                {
                    context.StudentVaccinations.AddRange(records);
                    await context.SaveChangesAsync();
                }
            }
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

            var cls1 = await context.SchoolClasses.FirstOrDefaultAsync(x => x.Code == "CLS001");
            var cls2 = await context.SchoolClasses.FirstOrDefaultAsync(x => x.Code == "CLS002");

            var u1 = await context.Users.FirstOrDefaultAsync(x => x.Code == "USR003");
            var u2 = await context.Users.FirstOrDefaultAsync(x => x.Code == "USR005");
            var u3 = await context.Users.FirstOrDefaultAsync(x => x.Code == "USR006");

            if (cls1 == null || cls2 == null || u1 == null || u2 == null || u3 == null) return;

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

            var allergy1 = await context.AllergyTypes.FirstOrDefaultAsync();
            var allergy2 = await context.AllergyTypes.Skip(1).FirstOrDefaultAsync();

            if (allergy1 != null && allergy2 != null)
            {
                context.StudentAllergies.AddRange(
                    new StudentAllergy { UserId = s1.UserId, AllergyId = allergy1.AllergyId, Note = "Nổi mề đay" },
                    new StudentAllergy { UserId = s2.UserId, AllergyId = allergy2.AllergyId, Note = "Mẩn đỏ" }
                );
            }

            var vac1 = await context.Vaccinations.FirstOrDefaultAsync();
            var vac2 = await context.Vaccinations.Skip(1).FirstOrDefaultAsync();

            if (vac1 != null && vac2 != null)
            {
                context.StudentVaccinations.AddRange(
                    new StudentVaccination { UserId = s1.UserId, VaccinationId = vac1.VaccinationId, Status = "DONE" },
                    new StudentVaccination { UserId = s1.UserId, VaccinationId = vac2.VaccinationId, Status = "DONE" }
                );
            }

            // Việc seed StudentVaccinations đã được chuyển sang đảm nhiệm tại SeedVaccinationCampaignsAsync 
            // để đảm bảo tính nhất quán với CampaignId và UpdatedAt.
            
            await context.SaveChangesAsync();
        }   

        private static async Task SeedHealthVisitsAsync(AppDbContext context)
        {
            if (await context.HealthVisits.AnyAsync()) return;

            var nurse = await context.Users.FirstOrDefaultAsync(x => x.Role == "NURSE");
            var student = await context.Students.FirstOrDefaultAsync();
            var dis = await context.DiseaseTypes.FirstOrDefaultAsync();
            var med1 = await context.Medicines.FirstOrDefaultAsync(x => x.Code == "MED001");
            var med2 = await context.Medicines.FirstOrDefaultAsync(x => x.Code == "MED002");

            if (nurse == null || student == null || dis == null || med1 == null || med2 == null) return;

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

            var admin = await context.Users.FirstOrDefaultAsync(x => x.Role == "ADMIN");
            var nurse = await context.Users.FirstOrDefaultAsync(x => x.Role == "NURSE");
            var studentUser = await context.Users.FirstOrDefaultAsync(x => x.Role == "STUDENT");

            if (admin == null || nurse == null || studentUser == null) return;

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