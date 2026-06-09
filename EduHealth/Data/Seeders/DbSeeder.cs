using EduHealth.Data.Entities;
using EduHealth.Helpers;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Data.Seeders
{
    public static class DbSeeder
    {
        private const string DefaultPassword = "123456";

        public static async Task SeedAdminAsync(AppDbContext context)
        {
            if (await context.Users.AnyAsync())
            {
                return;
            }

            var now = DateTime.UtcNow;
            var random = new Random(20260523);

            await using var tx = await context.Database.BeginTransactionAsync();

            var users = SeedUsers(context, now, random);
            await context.SaveChangesAsync();

            var admin = users.Single(x => x.Role == "ADMIN");
            var nurses = users.Where(x => x.Role == "NURSE").ToList();

            var classes = SeedClasses(context);
            await context.SaveChangesAsync();

            var students = SeedStudents(context, users, classes, random);
            var diseases = SeedDiseaseTypes(context);
            var allergyTypes = SeedAllergyTypes(context);
            var vaccinations = SeedVaccinations(context, now);
            var medicines = SeedMedicines(context, now);
            await context.SaveChangesAsync();

            SeedStudentAllergies(context, students, allergyTypes, random);
            await context.SaveChangesAsync();

            var medicineBatches = SeedMedicineBatches(context, medicines, nurses, now);
            await context.SaveChangesAsync();

            SeedMedicineStockInLogs(context, medicineBatches, nurses);
            await context.SaveChangesAsync();

            var visits = SeedHealthVisits(context, students, nurses, diseases, random, now);
            await context.SaveChangesAsync();

            SeedVisitPrescriptionsAndDispenseLogs(context, visits, medicines, medicineBatches, nurses, random, now);
            await context.SaveChangesAsync();

            SeedVaccinationCampaigns(context, admin, classes, students, vaccinations, random, now);
            await context.SaveChangesAsync();

            SeedNotifications(context, admin, nurses, students, classes, diseases, vaccinations, now);
            await context.SaveChangesAsync();

            await SeedMessagingAsync(context, admin, nurses, students, now);

            SeedSystemLogs(context, admin, nurses, now);
            await context.SaveChangesAsync();

            await tx.CommitAsync();
        }

        private static List<User> SeedUsers(AppDbContext context, DateTime now, Random random)
        {
            var users = new List<User>
            {
                new()
                {
                    Code = "USR001",
                    Username = "admin",
                    Phone = "0900000001",
                    Role = "ADMIN",
                    FullName = "Quản trị viên EduHealth",
                    IsActive = true,
                    Status = "ACTIVE",
                    CreatedAt = now,
                    UpdatedAt = now,
                    Email = "admin@eduhealth.local",
                    PasswordHash = PasswordHelper.HashPassword("123456Aa@"),
                    Gender = "OTHER"
                }
            };

            var nurseNames = new[]
            {
                ("Nguyễn Thị Lan", "FEMALE"),
                ("Trần Minh Khoa", "MALE"),
                ("Lê Thị Hồng Nhung", "FEMALE"),
                ("Phạm Quốc Bảo", "MALE"),
                ("Võ Thanh Mai", "FEMALE")
            };

            for (var i = 0; i < nurseNames.Length; i++)
            {
                users.Add(new User
                {
                    Code = $"USR{(i + 2):D3}",
                    Username = $"nurse{i + 1:D2}",
                    Phone = $"090000000{i + 2}",
                    Role = "NURSE",
                    FullName = nurseNames[i].Item1,
                    IsActive = true,
                    Status = "ACTIVE",
                    CreatedAt = now,
                    UpdatedAt = now,
                    Email = $"nurse{i + 1:D2}@eduhealth.local",
                    PasswordHash = PasswordHelper.HashPassword(DefaultPassword),
                    Gender = nurseNames[i].Item2
                });
            }

            var lastNames = new[] { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý", "Mai", "Trương" };
            var middleNames = new[] { "Văn", "Thị", "Minh", "Ngọc", "Gia", "Khánh", "Thanh", "Hoài", "Bảo", "Anh", "Quang", "Phương" };
            var firstNames = new[] { "An", "Bình", "Chi", "Dũng", "Hà", "Huy", "Khánh", "Lan", "Mai", "Nam", "Phúc", "Quân", "Trang", "Vy", "Giang", "Hiếu", "Khoa", "Linh", "Long", "Nhi", "Phát", "Sơn", "Thảo", "Uyên", "Yến" };
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var seq = 1; seq <= 305; seq++)
            {
                string fullName;
                var tries = 0;
                do
                {
                    fullName = $"{lastNames[random.Next(lastNames.Length)]} {middleNames[random.Next(middleNames.Length)]} {firstNames[random.Next(firstNames.Length)]}";
                    tries++;
                    if (tries > 6)
                    {
                        fullName = $"{fullName} {seq:D3}";
                    }
                } while (!usedNames.Add(fullName));

                users.Add(new User
                {
                    Code = $"USR{(seq + 6):D3}",
                    Username = $"HS{seq:D3}",
                    Phone = $"098{seq:D7}",
                    Role = "STUDENT",
                    FullName = fullName,
                    IsActive = true,
                    Status = "ACTIVE",
                    CreatedAt = now,
                    UpdatedAt = now,
                    Email = $"hs{seq:D3}@eduhealth.local",
                    PasswordHash = PasswordHelper.HashPassword(DefaultPassword),
                    Gender = seq % 2 == 0 ? "FEMALE" : "MALE"
                });
            }

            context.Users.AddRange(users);
            return users;
        }

        private static List<SchoolClass> SeedClasses(AppDbContext context)
        {
            var teacherNames = new[]
            {
                "Nguyễn Thị Thu Hà", "Trần Văn Minh", "Lê Thị Kim Anh", "Phạm Minh Châu", "Hoàng Thị Bích Ngọc",
                "Võ Quốc Huy", "Đặng Thị Thanh Tâm", "Bùi Minh Đức", "Đỗ Thị Hương Giang", "Hồ Anh Tuấn"
            };

            var classes = new List<SchoolClass>();
            var index = 0;

            for (var grade = 1; grade <= 5; grade++)
            {
                for (var section = 1; section <= 2; section++)
                {
                    classes.Add(new SchoolClass
                    {
                        Code = $"CLS{grade}{section:D2}",
                        ClassName = $"{grade}/{section}",
                        Grade = grade.ToString(),
                        TeacherName = teacherNames[index],
                        TeacherPhone = $"091{(index + 1):D7}"
                    });
                    index++;
                }
            }

            context.SchoolClasses.AddRange(classes);
            return classes;
        }

        private static List<Student> SeedStudents(AppDbContext context, List<User> users, List<SchoolClass> classes, Random random)
        {
            var guardians = new[] { "Nguyễn Văn Hùng", "Trần Thị Hoa", "Lê Minh Hải", "Phạm Thị Hạnh", "Hoàng Quốc Tuấn", "Võ Thị Thanh", "Đặng Minh Tâm", "Bùi Thị Hiền" };
            var studentUsers = users.Where(x => x.Role == "STUDENT").OrderBy(x => x.Code).ToList();
            var students = new List<Student>();
            var userIndex = 0;

            foreach (var cls in classes.OrderBy(x => x.Code))
            {
                var grade = int.Parse(cls.Grade!);
                var count = cls.ClassName.EndsWith("/1", StringComparison.Ordinal) ? 31 : 30;

                for (var ordinal = 1; ordinal <= count; ordinal++)
                {
                    var user = studentUsers[userIndex++];
                    var height = OneDecimal(104 + grade * 6 + random.NextDouble() * 10);
                    var weight = OneDecimal(15 + grade * 3 + random.NextDouble() * 7);
                    var birthYear = 2021 - grade;

                    students.Add(new Student
                    {
                        UserId = user.UserId,
                        Code = $"STD{userIndex:D3}",
                        ClassId = cls.ClassId,
                        FullName = user.FullName,
                        DateOfBirth = new DateTime(birthYear, random.Next(1, 13), random.Next(1, 28)),
                        CurrentHeight = height,
                        CurrentWeight = weight,
                        MedicalHistoryNotes = ordinal % 17 == 0 ? "Có tiền sử viêm mũi dị ứng, cần theo dõi khi giao mùa." : null,
                        Guardian = guardians[userIndex % guardians.Length],
                        Phone = $"097{userIndex:D7}"
                    });
                }
            }

            context.Students.AddRange(students);
            return students;
        }

        private static List<DiseaseType> SeedDiseaseTypes(AppDbContext context)
        {
            var diseases = new List<DiseaseType>
            {
                new() { Code = "DIS001", DiseaseName = "Cảm lạnh thông thường", Description = "Hắt hơi, sổ mũi, đau họng nhẹ.", IsContagious = true, StandardTreatment = "Nghỉ ngơi, uống nước ấm, theo dõi thân nhiệt." },
                new() { Code = "DIS002", DiseaseName = "Cúm mùa", Description = "Sốt, đau mỏi cơ, ho, mệt mỏi.", IsContagious = true, StandardTreatment = "Cách ly tạm thời, hạ sốt khi cần, báo phụ huynh." },
                new() { Code = "DIS003", DiseaseName = "Sốt nhẹ", Description = "Nhiệt độ tăng nhẹ chưa rõ nguyên nhân.", IsContagious = false, StandardTreatment = "Đo nhiệt độ định kỳ, bù nước, nghỉ tại phòng y tế." },
                new() { Code = "DIS004", DiseaseName = "Đau bụng", Description = "Đau bụng nhẹ, có thể do rối loạn tiêu hóa.", IsContagious = false, StandardTreatment = "Theo dõi triệu chứng, bù nước, liên hệ phụ huynh nếu nặng." },
                new() { Code = "DIS005", DiseaseName = "Viêm họng", Description = "Đau rát họng, ho, khó nuốt.", IsContagious = true, StandardTreatment = "Súc miệng nước muối, uống nước ấm, theo dõi." },
                new() { Code = "DIS006", DiseaseName = "Dị ứng da", Description = "Mẩn đỏ, ngứa, nổi mề đay.", IsContagious = false, StandardTreatment = "Tránh tác nhân nghi ngờ, theo dõi phản ứng dị ứng." },
                new() { Code = "DIS007", DiseaseName = "Đau đầu", Description = "Đau đầu do mệt, thiếu ngủ hoặc căng thẳng.", IsContagious = false, StandardTreatment = "Nghỉ ngơi, uống nước, theo dõi." },
                new() { Code = "DIS008", DiseaseName = "Chấn thương nhẹ", Description = "Trầy xước, bầm tím, va chạm nhẹ.", IsContagious = false, StandardTreatment = "Sát khuẩn, băng vết thương, chườm lạnh nếu cần." }
            };

            context.DiseaseTypes.AddRange(diseases);
            return diseases;
        }

        private static List<AllergyType> SeedAllergyTypes(AppDbContext context)
        {
            var allergies = new List<AllergyType>
            {
                new() { AllergyName = "Dị ứng hải sản", Severity = "HIGH" },
                new() { AllergyName = "Dị ứng thuốc penicillin", Severity = "HIGH" },
                new() { AllergyName = "Dị ứng phấn hoa", Severity = "MEDIUM" },
                new() { AllergyName = "Dị ứng bụi nhà", Severity = "MEDIUM" },
                new() { AllergyName = "Dị ứng sữa bò", Severity = "MEDIUM" },
                new() { AllergyName = "Dị ứng đậu phộng", Severity = "HIGH" },
                new() { AllergyName = "Dị ứng thời tiết", Severity = "LOW" }
            };

            context.AllergyTypes.AddRange(allergies);
            return allergies;
        }

        private static List<Vaccination> SeedVaccinations(AppDbContext context, DateTime now)
        {
            var vaccinations = new List<Vaccination>
            {
                new() { Name = "Sởi - Quai bị - Rubella", Description = "Vaccine MMR phòng sởi, quai bị và rubella.", CreatedAt = now },
                new() { Name = "Uốn ván - Bạch hầu", Description = "Vaccine phòng uốn ván và bạch hầu theo chương trình học đường.", CreatedAt = now },
                new() { Name = "Cúm mùa", Description = "Vaccine phòng cúm mùa hằng năm.", CreatedAt = now },
                new() { Name = "Viêm gan B", Description = "Vaccine phòng viêm gan B.", CreatedAt = now },
                new() { Name = "Thủy đậu", Description = "Vaccine phòng bệnh thủy đậu.", CreatedAt = now }
            };

            context.Vaccinations.AddRange(vaccinations);
            return vaccinations;
        }

        private static List<Medicine> SeedMedicines(AppDbContext context, DateTime now)
        {
            var specs = new[]
            {
                ("Paracetamol 500mg", "Paracetamol", "VIEN", "Hộp 10 vỉ x 10 viên", 80, 600, "Dùng hạ sốt, giảm đau thông thường.", 410),
                ("Ibuprofen 200mg", "Ibuprofen", "VIEN", "Hộp 10 vỉ x 10 viên", 50, 300, "Giảm đau, hạ sốt, kháng viêm.", 380),
                ("Oresol 245", "Glucose, Natri clorid, Kali clorid", "GOI", "Hộp 20 gói", 60, 420, "Bù nước và điện giải.", 360),
                ("Cetirizine 10mg", "Cetirizine dihydrochloride", "VIEN", "Hộp 10 vỉ x 10 viên", 40, 260, "Giảm triệu chứng dị ứng.", 520),
                ("Loratadine 10mg", "Loratadine", "VIEN", "Hộp 10 vỉ x 10 viên", 40, 240, "Kháng histamin điều trị dị ứng.", 500),
                ("Natri clorid 0.9% 500ml", "Sodium chloride", "CHAI", "Chai 500ml", 30, 120, "Rửa vết thương, vệ sinh ngoài da.", 330),
                ("Nước muối sinh lý 0.9% 10ml", "Sodium chloride", "ONG", "Hộp 40 ống", 80, 500, "Vệ sinh mắt, mũi.", 300),
                ("Povidone Iodine 10%", "Povidone iodine", "CHAI", "Chai 90ml", 20, 90, "Sát khuẩn ngoài da.", 420),
                ("Cồn y tế 70 độ", "Ethanol 70%", "CHAI", "Chai 500ml", 20, 100, "Sát khuẩn dụng cụ, bề mặt da.", 240),
                ("Gạc vô trùng 5x5cm", "Sterile gauze", "GOI", "Gói 10 miếng", 100, 800, "Che phủ vết thương nhỏ.", 730),
                ("Băng cuộn y tế 5cm", "Cotton bandage", "CUON", "Cuộn 5cm x 4.5m", 50, 360, "Băng bó vết thương.", 640),
                ("Băng cá nhân Urgo", "Adhesive bandage", "HOP", "Hộp 100 miếng", 50, 250, "Dán vết thương nhỏ.", 570),
                ("Dầu gió xanh", "Methyl salicylate, Menthol", "CHAI", "Chai 12ml", 30, 160, "Xoa ngoài khi đau nhức nhẹ.", 450),
                ("Vitamin C 500mg", "Ascorbic acid", "VIEN", "Lọ 100 viên", 50, 280, "Bổ sung vitamin C.", 690),
                ("Kẽm gluconate 10mg", "Zinc gluconate", "VIEN", "Hộp 30 viên", 30, 150, "Bổ sung kẽm.", 610),
                ("Men vi sinh Enterogermina", "Bacillus clausii", "ONG", "Hộp 20 ống", 25, 130, "Hỗ trợ rối loạn tiêu hóa.", 480),
                ("Smecta 3g", "Diosmectite", "GOI", "Hộp 30 gói", 40, 210, "Hỗ trợ tiêu chảy cấp.", 390),
                ("Berberin 10mg", "Berberine chloride", "VIEN", "Lọ 100 viên", 30, 180, "Hỗ trợ rối loạn tiêu hóa.", 350),
                ("Dextromethorphan 15mg", "Dextromethorphan", "VIEN", "Hộp 10 vỉ", 30, 170, "Giảm ho khan.", 430),
                ("Siro ho thảo dược", "Cao húng chanh, mật ong", "CHAI", "Chai 100ml", 20, 95, "Làm dịu ho, đau rát họng.", 270),
                ("Nhiệt kế điện tử", "Digital thermometer", "CAI", "Hộp 1 cái", 10, 35, "Đo thân nhiệt.", 900),
                ("Khẩu trang y tế", "Medical mask", "HOP", "Hộp 50 cái", 80, 600, "Phòng lây nhiễm đường hô hấp.", 180),
                ("Găng tay y tế nitrile", "Nitrile", "HOP", "Hộp 100 cái", 30, 160, "Bảo hộ khi chăm sóc y tế.", 280),
                ("Gel rửa tay khô 500ml", "Ethanol", "CHAI", "Chai 500ml", 25, 140, "Sát khuẩn tay nhanh.", 210),
                ("Dung dịch sát khuẩn Chlorhexidine", "Chlorhexidine gluconate", "CHAI", "Chai 250ml", 15, 80, "Sát khuẩn ngoài da.", 550)
            };

            var medicines = specs.Select((x, i) => new Medicine
            {
                Code = $"MED{i + 1:D3}",
                Name = x.Item1,
                ActiveIngredient = x.Item2,
                Unit = x.Item3,
                Packaging = x.Item4,
                WarningThreshold = x.Item5,
                StockQuantity = x.Item6,
                NearestExpiryDate = DateOnly.FromDateTime(now.AddDays(x.Item8)),
                Status = "ACTIVE",
                Note = x.Item7,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList();

            context.Medicines.AddRange(medicines);
            return medicines;
        }

        private static void SeedStudentAllergies(AppDbContext context, List<Student> students, List<AllergyType> allergyTypes, Random random)
        {
            var records = new List<StudentAllergy>();

            foreach (var student in students.Where((_, i) => i % 5 == 0))
            {
                var allergy = allergyTypes[random.Next(allergyTypes.Count)];
                records.Add(new StudentAllergy
                {
                    UserId = student.UserId,
                    AllergyId = allergy.AllergyId,
                    Note = allergy.Severity == "HIGH" ? "Cần báo nhân viên y tế trước khi dùng thuốc hoặc ăn bán trú." : "Theo dõi khi có biểu hiện bất thường."
                });
            }

            context.StudentAllergies.AddRange(records);
        }

        private static List<MedicineBatch> SeedMedicineBatches(
            AppDbContext context,
            List<Medicine> medicines,
            List<User> nurses,
            DateTime now)
        {
            var batches = new List<MedicineBatch>();

            foreach (var (medicine, medicineIndex) in medicines.Select((medicine, index) => (medicine, index)))
            {
                var firstQuantity = Math.Max(1, medicine.StockQuantity * 40 / 100);
                var secondQuantity = medicine.StockQuantity - firstQuantity;
                var baseExpiry = medicine.NearestExpiryDate ?? DateOnly.FromDateTime(now.AddDays(365));
                var quantities = secondQuantity > 0
                    ? new[] { firstQuantity, secondQuantity }
                    : new[] { firstQuantity };

                for (var batchIndex = 0; batchIndex < quantities.Length; batchIndex++)
                {
                    batches.Add(new MedicineBatch
                    {
                        Code = $"MBT{batches.Count + 1:D6}",
                        MedicineId = medicine.MedicineId,
                        BatchNumber = $"LOT-{now:yyyy}-{medicine.MedicineId:D3}-{batchIndex + 1:D2}",
                        ReceivedAt = now.AddDays(-120 + medicineIndex % 15 + batchIndex * 10),
                        ExpiryDate = baseExpiry.AddDays(batchIndex * 90),
                        InitialQuantity = quantities[batchIndex],
                        RemainingQuantity = quantities[batchIndex],
                        Status = "ACTIVE",
                        Note = batchIndex == 0 ? "FEFO priority batch" : "Supplemental batch",
                        CreatedByUserId = nurses[medicineIndex % nurses.Count].UserId,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
            }

            context.MedicineBatches.AddRange(batches);
            return batches;
        }

        private static void SeedMedicineStockInLogs(
            AppDbContext context,
            List<MedicineBatch> batches,
            List<User> nurses)
        {
            var stockByMedicine = new Dictionary<int, int>();
            var logs = new List<MedicineStockLog>();

            foreach (var (batch, index) in batches.Select((batch, index) => (batch, index)))
            {
                var stockBefore = stockByMedicine.GetValueOrDefault(batch.MedicineId);
                var stockAfter = stockBefore + batch.InitialQuantity;
                stockByMedicine[batch.MedicineId] = stockAfter;

                logs.Add(new MedicineStockLog
                {
                    MedicineId = batch.MedicineId,
                    MedicineBatchId = batch.MedicineBatchId,
                    UserId = batch.CreatedByUserId ?? nurses[index % nurses.Count].UserId,
                    Quantity = batch.InitialQuantity,
                    StockBefore = stockBefore,
                    StockAfter = stockAfter,
                    Reason = "Initial school-year stock",
                    ExpiryDate = batch.ExpiryDate,
                    BatchNumber = batch.BatchNumber,
                    CreatedAt = batch.ReceivedAt,
                    Type = "STOCK_IN",
                    Note = "Seed batch inventory"
                });
            }

            context.MedicineStockLogs.AddRange(logs);
        }

        private static void SeedMedicineStockInLogsLegacy(AppDbContext context, List<Medicine> medicines, List<User> nurses, DateTime now)
        {
            var logs = medicines.Select((medicine, i) => new MedicineStockLog
            {
                MedicineId = medicine.MedicineId,
                UserId = nurses[i % nurses.Count].UserId,
                Quantity = medicine.StockQuantity,
                StockBefore = 0,
                StockAfter = medicine.StockQuantity,
                Reason = "Nhập kho đầu năm học",
                ExpiryDate = medicine.NearestExpiryDate,
                BatchNumber = $"BATCH-{now:yyyy}-{i + 1:D3}",
                CreatedAt = now.AddDays(-20 + i % 10),
                Type = "STOCK_IN",
                Note = "Seed tồn kho ban đầu"
            }).ToList();

            context.MedicineStockLogs.AddRange(logs);
        }

        private static List<HealthVisit> SeedHealthVisits(AppDbContext context, List<Student> students, List<User> nurses, List<DiseaseType> diseases, Random random, DateTime now)
        {
            var visits = new List<HealthVisit>();
            var symptoms = new[]
            {
                "Sốt nhẹ, mệt mỏi",
                "Ho, sổ mũi, đau họng",
                "Đau bụng âm ỉ",
                "Đau đầu sau giờ ra chơi",
                "Trầy xước nhẹ ở đầu gối",
                "Nổi mẩn đỏ vùng tay",
                "Buồn nôn, chóng mặt"
            };

            foreach (var student in students.Where((_, i) => i % 3 == 0).Take(105))
            {
                var disease = diseases[random.Next(diseases.Count)];
                var measuredHeight = OneDecimal(student.CurrentHeight + random.NextDouble() * 1.2 - 0.6);
                var measuredWeight = OneDecimal(student.CurrentWeight + random.NextDouble() * 0.8 - 0.4);

                visits.Add(new HealthVisit
                {
                    Code = $"VIS{visits.Count + 1:D4}",
                    StudentUserId = student.UserId,
                    NurseId = nurses[visits.Count % nurses.Count].UserId,
                    VisitDate = now.AddDays(-random.Next(1, 90)).AddHours(random.Next(7, 16)),
                    Symptoms = symptoms[random.Next(symptoms.Length)],
                    Diagnosis = disease.DiseaseName,
                    Treatment = disease.StandardTreatment,
                    Note = "Đã xử lý tại phòng y tế và ghi nhận theo dõi.",
                    MeasuredHeight = measuredHeight,
                    MeasuredWeight = measuredWeight,
                    DiseaseId = disease.DiseaseId
                });
            }

            context.HealthVisits.AddRange(visits);
            return visits;
        }

        private static void SeedVisitPrescriptionsAndDispenseLogs(
            AppDbContext context,
            List<HealthVisit> visits,
            List<Medicine> medicines,
            List<MedicineBatch> medicineBatches,
            List<User> nurses,
            Random random,
            DateTime now)
        {
            var prescriptions = new List<VisitPrescription>();
            var dispenseLogs = new List<MedicineStockLog>();
            var batchesByMedicine = medicineBatches
                .GroupBy(x => x.MedicineId)
                .ToDictionary(
                    x => x.Key,
                    x => x.OrderBy(b => b.ExpiryDate).ThenBy(b => b.ReceivedAt).ToList());
            var totalStock = medicines.ToDictionary(x => x.MedicineId, x => x.StockQuantity);

            foreach (var visit in visits)
            {
                var selected = medicines
                    .Where(x => totalStock[x.MedicineId] > 0)
                    .OrderBy(_ => random.Next())
                    .Take(visit.VisitId % 3 == 0 ? 2 : 1)
                    .ToList();

                foreach (var medicine in selected)
                {
                    var requestedQuantity = medicine.Unit is "CHAI" or "HOP" or "CAI" ? 1 : random.Next(1, 4);
                    var quantity = Math.Min(requestedQuantity, totalStock[medicine.MedicineId]);
                    if (quantity <= 0)
                        continue;

                    prescriptions.Add(new VisitPrescription
                    {
                        VisitId = visit.VisitId,
                        MedicineId = medicine.MedicineId,
                        Quantity = quantity,
                        UsageIns = medicine.Unit == "VIEN"
                            ? "Uống sau ăn theo hướng dẫn nhân viên y tế."
                            : "Sử dụng theo hướng dẫn tại phòng y tế."
                    });

                    var remainingToDispense = quantity;
                    foreach (var batch in batchesByMedicine[medicine.MedicineId])
                    {
                        if (remainingToDispense == 0)
                            break;
                        if (batch.Status != "ACTIVE" || batch.RemainingQuantity <= 0)
                            continue;

                        var dispensed = Math.Min(batch.RemainingQuantity, remainingToDispense);
                        var stockBefore = totalStock[medicine.MedicineId];
                        batch.RemainingQuantity -= dispensed;
                        batch.Status = batch.RemainingQuantity == 0 ? "DEPLETED" : "ACTIVE";
                        batch.UpdatedAt = now;
                        totalStock[medicine.MedicineId] -= dispensed;
                        remainingToDispense -= dispensed;

                        dispenseLogs.Add(new MedicineStockLog
                        {
                            MedicineId = medicine.MedicineId,
                            MedicineBatchId = batch.MedicineBatchId,
                            UserId = nurses[(visit.VisitId + medicine.MedicineId) % nurses.Count].UserId,
                            Quantity = dispensed,
                            StockBefore = stockBefore,
                            StockAfter = totalStock[medicine.MedicineId],
                            Reason = "Dispensed for seeded examination",
                            ExpiryDate = batch.ExpiryDate,
                            BatchNumber = batch.BatchNumber,
                            CreatedAt = visit.VisitDate,
                            Type = "DISPENSE",
                            VisitId = visit.VisitId,
                            Note = $"Seed dispense for {visit.Code}"
                        });
                    }
                }
            }

            foreach (var medicine in medicines)
            {
                medicine.StockQuantity = totalStock[medicine.MedicineId];
                medicine.NearestExpiryDate = batchesByMedicine[medicine.MedicineId]
                    .Where(x => x.Status == "ACTIVE" && x.RemainingQuantity > 0)
                    .OrderBy(x => x.ExpiryDate)
                    .Select(x => (DateOnly?)x.ExpiryDate)
                    .FirstOrDefault();
                medicine.UpdatedAt = now;
            }

            context.VisitPrescriptions.AddRange(prescriptions);
            context.MedicineStockLogs.AddRange(dispenseLogs);
        }

        private static void SeedVisitPrescriptionsAndDispenseLogsLegacy(AppDbContext context, List<HealthVisit> visits, List<Medicine> medicines, List<User> nurses, Random random, DateTime now)
        {
            var prescriptions = new List<VisitPrescription>();
            var dispenseLogs = new List<MedicineStockLog>();
            var stock = medicines.ToDictionary(x => x.MedicineId, x => x.StockQuantity);

            foreach (var visit in visits)
            {
                var selected = medicines
                    .OrderBy(_ => random.Next())
                    .Take(visit.VisitId % 3 == 0 ? 2 : 1)
                    .ToList();

                foreach (var medicine in selected)
                {
                    var quantity = medicine.Unit is "CHAI" or "HOP" or "CAI" ? 1 : random.Next(1, 4);
                    var before = stock[medicine.MedicineId];
                    var after = Math.Max(0, before - quantity);
                    stock[medicine.MedicineId] = after;

                    prescriptions.Add(new VisitPrescription
                    {
                        VisitId = visit.VisitId,
                        MedicineId = medicine.MedicineId,
                        Quantity = quantity,
                        UsageIns = medicine.Unit == "VIEN" ? "Uống sau ăn theo hướng dẫn nhân viên y tế." : "Sử dụng theo hướng dẫn tại phòng y tế."
                    });

                    dispenseLogs.Add(new MedicineStockLog
                    {
                        MedicineId = medicine.MedicineId,
                        UserId = nurses[(visit.VisitId + medicine.MedicineId) % nurses.Count].UserId,
                        Quantity = quantity,
                        StockBefore = before,
                        StockAfter = after,
                        Reason = "Cấp thuốc theo lượt khám",
                        ExpiryDate = medicine.NearestExpiryDate,
                        BatchNumber = $"BATCH-{now:yyyy}-{medicine.MedicineId:D3}",
                        CreatedAt = visit.VisitDate,
                        Type = "DISPENSE",
                        VisitId = visit.VisitId,
                        Note = $"Cấp thuốc cho phiếu {visit.Code}"
                    });
                }
            }

            foreach (var medicine in medicines)
            {
                medicine.StockQuantity = stock[medicine.MedicineId];
                medicine.UpdatedAt = now;
            }

            context.VisitPrescriptions.AddRange(prescriptions);
            context.MedicineStockLogs.AddRange(dispenseLogs);
        }

        private static void SeedVaccinationCampaigns(AppDbContext context, User admin, List<SchoolClass> classes, List<Student> students, List<Vaccination> vaccinations, Random random, DateTime now)
        {
            var campaignSpecs = new[]
            {
                (Code: "VAC2026-01", Name: "Chiến dịch tiêm vaccine Cúm mùa năm học 2026", Vaccine: vaccinations.Single(x => x.Name == "Cúm mùa"), Dose: 1, Date: DateOnly.FromDateTime(now.AddDays(14))),
                (Code: "VAC2026-02", Name: "Chiến dịch tiêm Sởi - Quai bị - Rubella", Vaccine: vaccinations.Single(x => x.Name == "Sởi - Quai bị - Rubella"), Dose: 1, Date: DateOnly.FromDateTime(now.AddDays(45))),
                (Code: "VAC2026-03", Name: "Chiến dịch nhắc lại Uốn ván - Bạch hầu", Vaccine: vaccinations.Single(x => x.Name == "Uốn ván - Bạch hầu"), Dose: 2, Date: DateOnly.FromDateTime(now.AddDays(-25)))
            };

            foreach (var spec in campaignSpecs)
            {
                var campaign = new VaccinationCampaign
                {
                    Code = spec.Code,
                    Name = spec.Name,
                    VaccineName = spec.Vaccine.Name,
                    DoseNumber = spec.Dose,
                    ScheduledDate = spec.Date,
                    TargetType = "CLASS",
                    Status = spec.Date < DateOnly.FromDateTime(now) ? "COMPLETED" : "ACTIVE",
                    Note = "Dữ liệu seed cho chiến dịch tiêm chủng toàn trường.",
                    CreatedByUserId = admin.UserId,
                    CreatedAt = now.AddDays(-40)
                };

                context.VaccinationCampaigns.Add(campaign);
                context.SaveChanges();

                context.VaccinationCampaignTargetClasses.AddRange(classes.Select(cls => new VaccinationCampaignTargetClass
                {
                    CampaignId = campaign.CampaignId,
                    ClassId = cls.ClassId
                }));

                var records = students.Select(student =>
                {
                    var status = campaign.Status == "COMPLETED"
                        ? random.Next(100) switch
                        {
                            < 82 => "DONE",
                            < 90 => "ABSENT",
                            < 96 => "POSTPONED",
                            _ => "CONTRAINDICATED"
                        }
                        : random.Next(100) switch
                        {
                            < 76 => "PENDING",
                            < 86 => "POSTPONED",
                            < 94 => "DONE",
                            _ => "CONTRAINDICATED"
                        };

                    return new StudentVaccination
                    {
                        UserId = student.UserId,
                        CampaignId = campaign.CampaignId,
                        VaccinationId = spec.Vaccine.VaccinationId,
                        Status = status,
                        VaccinatedAt = status == "DONE" ? spec.Date : null,
                        LotNumber = status == "DONE" ? $"LOT-{spec.Code}-{student.UserId % 50 + 1:D2}" : null,
                        Note = status == "CONTRAINDICATED" ? "Chống chỉ định tạm thời theo khai báo y tế." : "Dữ liệu seed.",
                        UpdatedAt = now
                    };
                }).ToList();

                context.StudentVaccinations.AddRange(records);
            }
        }

        private static void SeedNotifications(AppDbContext context, User admin, List<User> nurses, List<Student> students, List<SchoolClass> classes, List<DiseaseType> diseases, List<Vaccination> vaccinations, DateTime now)
        {
            var notifications = new List<Notification>
            {
                new()
                {
                    Title = "Lịch kiểm tra sức khỏe định kỳ học kỳ II",
                    Content = "Nhà trường tổ chức kiểm tra chiều cao, cân nặng và sức khỏe tổng quát cho học sinh trong tuần tới.",
                    Type = "HEALTH_CHECK",
                    Visibility = "BOTH",
                    Status = "PUBLISHED",
                    CreatedByUserId = admin.UserId,
                    CreatedAt = now.AddDays(-5),
                    PublishedAt = now.AddDays(-5),
                    ClassId = classes.First().ClassId
                },
                new()
                {
                    Title = "Khuyến cáo phòng cúm mùa",
                    Content = "Phụ huynh vui lòng nhắc học sinh rửa tay thường xuyên, đeo khẩu trang khi có triệu chứng ho sốt.",
                    Type = "DISEASE_ALERT",
                    Visibility = "PUBLIC",
                    Status = "PUBLISHED",
                    CreatedByUserId = admin.UserId,
                    CreatedAt = now.AddDays(-3),
                    PublishedAt = now.AddDays(-3),
                    DiseaseId = diseases.Single(x => x.Code == "DIS002").DiseaseId
                },
                new()
                {
                    Title = "Thông báo chiến dịch tiêm vaccine cúm mùa",
                    Content = "Nhà trường triển khai chiến dịch tiêm vaccine cúm mùa cho học sinh toàn trường.",
                    Type = "VACCINATION",
                    Visibility = "INTERNAL",
                    Status = "PUBLISHED",
                    CreatedByUserId = admin.UserId,
                    CreatedAt = now.AddDays(-2),
                    PublishedAt = now.AddDays(-2),
                    VaccinationId = vaccinations.Single(x => x.Name == "Cúm mùa").VaccinationId
                },
                new()
                {
                    Title = "Dự thảo thông báo họp phụ huynh về y tế học đường",
                    Content = "Nội dung dự kiến trao đổi về quy trình chăm sóc sức khỏe học sinh.",
                    Type = "GENERAL",
                    Visibility = "INTERNAL",
                    Status = "DRAFT",
                    CreatedByUserId = admin.UserId,
                    CreatedAt = now.AddDays(-1)
                }
            };

            context.Notifications.AddRange(notifications);
            context.SaveChanges();

            var internalUsers = nurses.Concat(students.Take(60).Select(x => x.User)).ToList();
            var recipients = new List<NotificationRecipient>();

            foreach (var notification in notifications.Where(x => x.Status == "PUBLISHED" && x.Visibility != "PUBLIC"))
            {
                foreach (var user in internalUsers)
                {
                    recipients.Add(new NotificationRecipient
                    {
                        NotificationId = notification.NotificationId,
                        UserId = user.UserId,
                        IsRead = user.UserId % 4 == 0,
                        ReadAt = user.UserId % 4 == 0 ? now.AddHours(-user.UserId % 24) : null,
                        SentAt = notification.PublishedAt ?? notification.CreatedAt,
                        Status = "SENT"
                    });
                }
            }

            context.NotificationRecipients.AddRange(recipients);
        }

        private static async Task SeedMessagingAsync(AppDbContext context, User admin, List<User> nurses, List<Student> students, DateTime now)
        {
            var conversations = new List<Conversation>();

            for (var i = 0; i < 8; i++)
            {
                conversations.Add(new Conversation
                {
                    ConversationType = "DIRECT",
                    StudentUserId = students[i].UserId,
                    Title = $"Trao đổi sức khỏe học sinh {students[i].FullName}",
                    CreatedByUserId = nurses[i % nurses.Count].UserId,
                    CreatedAt = now.AddDays(-8 + i),
                    UpdatedAt = now.AddDays(-7 + i)
                });
            }

            conversations.Add(new Conversation
            {
                ConversationType = "GROUP",
                Title = "Tổ y tế trường Tiểu học EduHealth",
                CreatedByUserId = admin.UserId,
                CreatedAt = now.AddDays(-10),
                UpdatedAt = now.AddDays(-1)
            });

            context.Conversations.AddRange(conversations);
            await context.SaveChangesAsync();

            var participants = new List<ConversationParticipant>();
            foreach (var conversation in conversations.Take(8))
            {
                participants.Add(new ConversationParticipant
                {
                    ConversationId = conversation.ConversationId,
                    UserId = conversation.CreatedByUserId,
                    RoleInConversation = "NURSE",
                    JoinedAt = conversation.CreatedAt,
                    LastReadAt = now.AddDays(-1),
                    IsPinned = conversation.ConversationId % 2 == 0
                });

                participants.Add(new ConversationParticipant
                {
                    ConversationId = conversation.ConversationId,
                    UserId = conversation.StudentUserId!.Value,
                    RoleInConversation = "STUDENT",
                    JoinedAt = conversation.CreatedAt,
                    LastReadAt = now.AddDays(-2),
                    IsPinned = false
                });
            }

            var group = conversations.Last();
            participants.Add(new ConversationParticipant
            {
                ConversationId = group.ConversationId,
                UserId = admin.UserId,
                RoleInConversation = "ADMIN",
                JoinedAt = group.CreatedAt,
                LastReadAt = now,
                IsPinned = true
            });
            participants.AddRange(nurses.Select(nurse => new ConversationParticipant
            {
                ConversationId = group.ConversationId,
                UserId = nurse.UserId,
                RoleInConversation = "NURSE",
                JoinedAt = group.CreatedAt,
                LastReadAt = now.AddHours(-2),
                IsPinned = false
            }));

            context.ConversationParticipants.AddRange(participants);
            await context.SaveChangesAsync();

            var messages = new List<ChatMessage>();
            foreach (var conversation in conversations.Take(8))
            {
                messages.Add(new ChatMessage
                {
                    ConversationId = conversation.ConversationId,
                    SenderUserId = conversation.CreatedByUserId,
                    Content = "Nhân viên y tế đã ghi nhận tình trạng sức khỏe của học sinh, phụ huynh vui lòng theo dõi thêm tại nhà.",
                    MessageType = "TEXT",
                    SentAt = conversation.CreatedAt.AddMinutes(12)
                });

                messages.Add(new ChatMessage
                {
                    ConversationId = conversation.ConversationId,
                    SenderUserId = conversation.StudentUserId!.Value,
                    Content = "Em đã báo lại với phụ huynh và sẽ tiếp tục theo dõi triệu chứng.",
                    MessageType = "TEXT",
                    SentAt = conversation.CreatedAt.AddMinutes(25)
                });
            }

            messages.Add(new ChatMessage
            {
                ConversationId = group.ConversationId,
                SenderUserId = admin.UserId,
                Content = "Tuần này tổ y tế kiểm tra lại hạn dùng thuốc và cập nhật tồn kho trước thứ Sáu.",
                MessageType = "TEXT",
                SentAt = now.AddDays(-2)
            });

            context.ChatMessages.AddRange(messages);
            await context.SaveChangesAsync();

            var attachmentMessage = messages.First();
            context.ChatMessageAttachments.Add(new ChatMessageAttachment
            {
                MessageId = attachmentMessage.MessageId,
                FileName = "health-note-sample.pdf",
                OriginalFileName = "Phieu-theo-doi-suc-khoe-mau.pdf",
                FileUrl = "https://example.com/eduhealth/health-note-sample.pdf",
                ContentType = "application/pdf",
                SizeBytes = 245_760,
                UploadedByUserId = attachmentMessage.SenderUserId,
                UploadedAt = attachmentMessage.SentAt.AddMinutes(1)
            });

            foreach (var conversation in conversations)
            {
                conversation.LastMessageId = messages
                    .Where(x => x.ConversationId == conversation.ConversationId)
                    .OrderByDescending(x => x.SentAt)
                    .Select(x => x.MessageId)
                    .FirstOrDefault();
            }

            await context.SaveChangesAsync();
        }

        private static void SeedSystemLogs(AppDbContext context, User admin, List<User> nurses, DateTime now)
        {
            context.SystemLogs.AddRange(
                new SystemLog
                {
                    CreatedAt = now.AddDays(-20),
                    ActorUserId = admin.UserId,
                    ActorName = admin.FullName,
                    ActorUsername = admin.Username,
                    ActorRole = admin.Role,
                    Module = "Seed",
                    Action = "InitializeSchoolData",
                    TargetType = "Database",
                    TargetId = "EduHealthDb",
                    TargetLabel = "Khởi tạo dữ liệu trường học",
                    Description = "Seed dữ liệu demo cho trường 5 khối, 10 lớp, 305 học sinh.",
                    Status = "SUCCESS",
                    MetadataJson = "{\"source\":\"DbSeeder\"}"
                },
                new SystemLog
                {
                    CreatedAt = now.AddDays(-10),
                    ActorUserId = nurses.First().UserId,
                    ActorName = nurses.First().FullName,
                    ActorUsername = nurses.First().Username,
                    ActorRole = nurses.First().Role,
                    Module = "Medicine",
                    Action = "StockIn",
                    TargetType = "MedicineStock",
                    TargetId = "INITIAL",
                    TargetLabel = "Nhập kho đầu năm học",
                    Description = "Ghi nhận tồn kho ban đầu cho tủ thuốc y tế học đường.",
                    Status = "SUCCESS",
                    MetadataJson = "{\"type\":\"STOCK_IN\"}"
                });
        }

        private static float OneDecimal(double value)
        {
            return (float)Math.Round(value, 1, MidpointRounding.AwayFromZero);
        }
    }
}
