using EduHealth.Data;
using EduHealth.Data.Entities;
using EduHealth.DTOs.Examinations;
using EduHealth.Helpers;
using EduHealth.Repositories.Interfaces;
using EduHealth.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace EduHealth.Services.Implementations
{
    public class ExaminationService : IExaminationService
    {
        private readonly AppDbContext _context;
        private readonly IExaminationRepository _examinationRepository;
        private readonly ISystemLogWriter _logWriter;

        public ExaminationService(AppDbContext context, IExaminationRepository examinationRepository, ISystemLogWriter logWriter)
        {
            _context = context;
            _examinationRepository = examinationRepository;
            _logWriter = logWriter;
        }

        public async Task<(IReadOnlyList<ExaminationListItemDto> Items, int TotalItems, int TotalPages, int Page, int PageSize)> GetPagedAsync(
            ExaminationListQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);

            var (items, total) = await _examinationRepository.GetPagedAsync(
                query.StudentId,
                query.ClassId,
                query.FromDate,
                query.ToDate,
                query.DiseaseTypeId,
                page,
                pageSize,
                cancellationToken);

            var dtos = items.Select(x => new ExaminationListItemDto
            {
                Id = x.Code,
                VisitDate = x.VisitDate,
                Student = new ExaminationStudentBriefDto
                {
                    StudentId = x.Student.Code,
                    StudentCode = string.Empty, // Placeholder for future StudentCode
                    FullName = x.Student.FullName,
                    ClassId = x.Student.Class.Code,
                    ClassName = x.Student.Class.ClassName
                },
                Nurse = new ExaminationUserBriefDto
                {
                    UserId = x.Nurse.Code,
                    FullName = x.Nurse.FullName
                },
                DiseaseType = x.DiseaseType is null ? null : new ExaminationDiseaseBriefDto { Id = x.DiseaseType.Code, Name = x.DiseaseType.DiseaseName },
                Symptoms = x.Symptoms ?? string.Empty,
                Diagnosis = x.Diagnosis ?? string.Empty,
                HasPrescription = x.VisitPrescriptions.Count > 0
            }).ToList();

            var totalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize);
            return (dtos, total, totalPages, page, pageSize);
        }

        public async Task<ExaminationDetailDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            var visit = await _examinationRepository.GetByCodeAsync(id.Trim(), cancellationToken);
            if (visit is null)
                return null;

            return new ExaminationDetailDto
            {
                Id = visit.Code,
                VisitDate = visit.VisitDate,
                Student = new ExaminationStudentDetailDto
                {
                    StudentId = visit.Student.Code,
                    StudentCode = string.Empty, // Placeholder for future StudentCode
                    FullName = visit.Student.FullName,
                    ClassId = visit.Student.Class.Code,
                    ClassName = visit.Student.Class.ClassName,
                    Gender = visit.Student.User.Gender
                },
                Nurse = new ExaminationUserBriefDto
                {
                    UserId = visit.Nurse.Code,
                    FullName = visit.Nurse.FullName
                },
                DiseaseType = visit.DiseaseType is null ? null : new ExaminationDiseaseBriefDto { Id = visit.DiseaseType.Code, Name = visit.DiseaseType.DiseaseName },
                Symptoms = visit.Symptoms ?? string.Empty,
                Diagnosis = visit.Diagnosis ?? string.Empty,
                Treatment = visit.Treatment ?? string.Empty,
                Note = visit.Note,
                Prescriptions = visit.VisitPrescriptions.Select(p => new ExaminationPrescriptionItemDto
                {
                    PrescriptionId = $"VP{p.PrescriptionId:D3}",
                    MedicineId = p.Medicine.Code,
                    MedicineName = p.Medicine.Name,
                    Quantity = p.Quantity,
                    Dosage = p.UsageIns,
                    UsageInstruction = p.UsageIns
                }).ToList(),
                CreatedAt = visit.VisitDate
            };
        }

        public async Task<(bool Success, int? StatusCode, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, CreateExaminationResponseDto? Data)> CreateAsync(
            int nurseUserId,
            CreateExaminationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<(string Field, string Code, string Message)>();

            if (nurseUserId <= 0)
            {
                return (false, 401, "Phiên đăng nhập không hợp lệ.", new[] { ("nurseId", "UNAUTHORIZED", "Không xác định được y tá đang đăng nhập.") }, null);
            }

            if (string.IsNullOrWhiteSpace(request.StudentId))
                errors.Add(("studentId", "REQUIRED", "studentId bắt buộc."));

            if (request.VisitDate == default)
                errors.Add(("visitDate", "REQUIRED", "visitDate bắt buộc."));

            if (string.IsNullOrWhiteSpace(request.Symptoms))
                errors.Add(("symptoms", "REQUIRED", "symptoms bắt buộc."));

            if (string.IsNullOrWhiteSpace(request.Diagnosis))
                errors.Add(("diagnosis", "REQUIRED", "diagnosis bắt buộc."));

            if (string.IsNullOrWhiteSpace(request.Treatment))
                errors.Add(("treatment", "REQUIRED", "treatment bắt buộc."));

            if (request.Prescriptions is not null)
            {
                for (var i = 0; i < request.Prescriptions.Count; i++)
                {
                    var item = request.Prescriptions[i];
                    if (string.IsNullOrWhiteSpace(item.MedicineId))
                        errors.Add(($"prescriptions[{i}].medicineId", "REQUIRED", "medicineId bắt buộc."));
                    if (item.Quantity <= 0)
                        errors.Add(($"prescriptions[{i}].quantity", "INVALID_QUANTITY", "quantity phải > 0."));
                }
            }

            if (errors.Count > 0)
                return (false, 400, "Dữ liệu không hợp lệ.", errors, null);

            var student = await _examinationRepository.GetStudentByCodeAsync(request.StudentId.Trim(), cancellationToken);
            if (student is null)
            {
                return (false, 404, "Không tìm thấy dữ liệu liên quan.", new[] { ("studentId", "STUDENT_NOT_FOUND", "studentId không tồn tại.") }, null);
            }

            var nurse = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == nurseUserId, cancellationToken);

            if (nurse is null)
            {
                return (false, 404, "Không tìm thấy dữ liệu liên quan.", new[] { ("nurseId", "NURSE_NOT_FOUND", "Không tìm thấy tài khoản y tá.") }, null);
            }

            DiseaseType? disease = null;
            if (request.DiseaseId.HasValue)
            {
                disease = await _examinationRepository.GetDiseaseTypeByIdAsync(request.DiseaseId.Value, cancellationToken);
                if (disease is null)
                    return (false, 404, "Không tìm thấy dữ liệu liên quan.", new[] { ("diseaseId", "DISEASE_TYPE_NOT_FOUND", "diseaseId không tồn tại.") }, null);
            }
            else if (!string.IsNullOrWhiteSpace(request.DiseaseTypeId))
            {
                disease = await _examinationRepository.GetDiseaseTypeByCodeAsync(request.DiseaseTypeId.Trim(), cancellationToken);
                if (disease is null)
                    return (false, 404, "Không tìm thấy dữ liệu liên quan.", new[] { ("diseaseTypeId", "DISEASE_TYPE_NOT_FOUND", "diseaseTypeId không tồn tại.") }, null);
            }

            var prescriptions = request.Prescriptions?.ToList() ?? new List<CreateExaminationPrescriptionItemDto>();

            // Transaction
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var visit = new HealthVisit
                {
                    Code = string.Empty,
                    StudentUserId = student.UserId,
                    NurseId = nurseUserId,
                    VisitDate = request.VisitDate,
                    DiseaseId = disease?.DiseaseId,
                    Symptoms = request.Symptoms.Trim(),
                    Diagnosis = request.Diagnosis.Trim(),
                    Treatment = request.Treatment.Trim(),
                    Note = request.Note?.Trim(),
                    MeasuredHeight = 0,
                    MeasuredWeight = 0
                };

                await _examinationRepository.AddVisitAsync(visit, cancellationToken);
                await _examinationRepository.SaveChangesAsync(cancellationToken);

                var (codeAssigned, codeError) = await AssignUniqueVisitCodeAsync(visit, cancellationToken);
                if (!codeAssigned)
                {
                    return (false, 409, "Không thể tạo mã phiếu khám. Vui lòng thử lại.", new[] { ("code", "VIS_CODE_CONFLICT", codeError ?? "Mã phiếu khám bị trùng.") }, null);
                }

                var createdPrescriptions = new List<VisitPrescription>();
                var inventoryMovements = new List<MedicineStockLog>();
                var meds = new Dictionary<string, Medicine>(StringComparer.OrdinalIgnoreCase);
                var availableBatches = new Dictionary<int, List<MedicineBatch>>();

                if (prescriptions.Count > 0)
                {
                    // Load medicines and check stock
                    for (var i = 0; i < prescriptions.Count; i++)
                    {
                        var med = await _examinationRepository.GetMedicineByCodeAsync(prescriptions[i].MedicineId, cancellationToken);
                        if (med is null)
                        {
                            return (false, 404, "Không tìm thấy dữ liệu liên quan.", new[] { ($"prescriptions[{i}].medicineId", "MEDICINE_NOT_FOUND", "medicineId không tồn tại.") }, null);
                        }

                        if (!string.Equals(med.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                        {
                            return (false, 400, "Dữ liệu không hợp lệ.", new[] { ($"prescriptions[{i}].medicineId", "MEDICINE_INACTIVE", "Thuốc đang INACTIVE không được chọn.") }, null);
                        }

                        meds[med.Code] = med;
                    }

                    var requestedByMedicine = prescriptions
                        .GroupBy(x => x.MedicineId, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(x => x.Key, x => x.Sum(y => y.Quantity), StringComparer.OrdinalIgnoreCase);

                    for (var i = 0; i < prescriptions.Count; i++)
                    {
                        var item = prescriptions[i];
                        var med = meds[item.MedicineId];

                        if (!availableBatches.TryGetValue(med.MedicineId, out var batches))
                        {
                            var today = VietnamTimeHelper.TodayDateOnly;
                            batches = await _context.MedicineBatches
                                .Where(x =>
                                    x.MedicineId == med.MedicineId &&
                                    x.Status == "ACTIVE" &&
                                    x.RemainingQuantity > 0 &&
                                    x.ExpiryDate >= today)
                                .OrderBy(x => x.ExpiryDate)
                                .ThenBy(x => x.ReceivedAt)
                                .ToListAsync(cancellationToken);
                            availableBatches[med.MedicineId] = batches;
                        }

                        if (requestedByMedicine[item.MedicineId] > batches.Sum(x => x.RemainingQuantity))
                        {
                            return (false, 400, "Dữ liệu không hợp lệ.", new[] { ($"prescriptions[{i}].quantity", "INSUFFICIENT_STOCK", "Số lượng thuốc trong kho không đủ.") }, null);
                        }
                    }

                    foreach (var item in prescriptions)
                    {
                        var med = meds[item.MedicineId];
                        var stockBefore = availableBatches[med.MedicineId]
                            .Where(x => x.Status == "ACTIVE" && x.RemainingQuantity > 0)
                            .Sum(x => x.RemainingQuantity);
                        var stockCursor = stockBefore;
                        var remainingToDispense = item.Quantity;
                        var now = VietnamTimeHelper.Now;

                        createdPrescriptions.Add(new VisitPrescription
                        {
                            VisitId = visit.VisitId,
                            MedicineId = med.MedicineId,
                            Quantity = item.Quantity,
                            UsageIns = item.UsageInstruction
                        });

                        foreach (var batch in availableBatches[med.MedicineId])
                        {
                            if (remainingToDispense == 0)
                                break;

                            var quantity = Math.Min(batch.RemainingQuantity, remainingToDispense);
                            batch.RemainingQuantity -= quantity;
                            batch.Status = batch.RemainingQuantity == 0 ? "DEPLETED" : "ACTIVE";
                            batch.UpdatedAt = now;
                            remainingToDispense -= quantity;
                            stockCursor -= quantity;

                            inventoryMovements.Add(new MedicineStockLog
                            {
                                MedicineId = med.MedicineId,
                                MedicineBatchId = batch.MedicineBatchId,
                                UserId = nurseUserId,
                                Quantity = quantity,
                                StockBefore = stockCursor + quantity,
                                StockAfter = stockCursor,
                                Type = "DISPENSE",
                                Reason = null,
                                ExpiryDate = batch.ExpiryDate,
                                BatchNumber = batch.BatchNumber,
                                Note = null,
                                VisitId = visit.VisitId,
                                CreatedAt = now
                            });
                        }

                        med.StockQuantity = availableBatches[med.MedicineId]
                            .Where(x => x.Status == "ACTIVE" && x.RemainingQuantity > 0)
                            .Sum(x => x.RemainingQuantity);
                        med.NearestExpiryDate = availableBatches[med.MedicineId]
                            .Where(x => x.Status == "ACTIVE" && x.RemainingQuantity > 0)
                            .OrderBy(x => x.ExpiryDate)
                            .Select(x => (DateOnly?)x.ExpiryDate)
                            .FirstOrDefault();
                        med.UpdatedAt = now;
                    }

                    await _examinationRepository.AddPrescriptionsAsync(createdPrescriptions, cancellationToken);
                    await _examinationRepository.AddStockLogsAsync(inventoryMovements, cancellationToken);

                    // update medicines
                    foreach (var med in meds.Values)
                    {
                        _context.Medicines.Update(med);
                    }

                    await _examinationRepository.SaveChangesAsync(cancellationToken);
                }

                await tx.CommitAsync(cancellationToken);

                // Load nurse for response
                var response = new CreateExaminationResponseDto
                {
                    Id = visit.Code,
                    VisitDate = visit.VisitDate,
                    Student = new ExaminationStudentSimpleDto
                    {
                        StudentId = student.Code,
                        StudentCode = student.Code,
                        FullName = student.FullName
                    },
                    Nurse = new ExaminationUserBriefDto
                    {
                        UserId = nurse.Code,
                        FullName = nurse.FullName
                    },
                    DiseaseType = disease is null ? null : new ExaminationDiseaseBriefDto { Id = disease.Code, Name = disease.DiseaseName },
                    Symptoms = visit.Symptoms ?? string.Empty,
                    Diagnosis = visit.Diagnosis ?? string.Empty,
                    Treatment = visit.Treatment ?? string.Empty,
                    Note = visit.Note,
                    Prescriptions = createdPrescriptions.Select(p =>
                    {
                        var med = meds.Values.FirstOrDefault(m => m.MedicineId == p.MedicineId);
                        return new ExaminationPrescriptionItemDto
                        {
                            PrescriptionId = $"VP{p.PrescriptionId:D3}",
                            MedicineId = med?.Code ?? string.Empty,
                            MedicineName = med?.Name ?? string.Empty,
                            Quantity = p.Quantity,
                            Dosage = null,
                            UsageInstruction = p.UsageIns
                        };
                    }).ToList(),
                    InventoryMovements = inventoryMovements.Select(m =>
                    {
                        var med = meds.Values.FirstOrDefault(x => x.MedicineId == m.MedicineId);
                        return new ExaminationInventoryMovementDto
                        {
                            MovementId = $"MSL{m.LogId:D3}",
                            MedicineId = med?.Code ?? string.Empty,
                            BatchId = availableBatches.TryGetValue(m.MedicineId, out var batches)
                                ? batches.FirstOrDefault(x => x.MedicineBatchId == m.MedicineBatchId)?.Code
                                : null,
                            Type = m.Type,
                            Quantity = m.Quantity
                        };
                    }).ToList(),
                    CreatedAt = VietnamTimeHelper.Now
                };

                await _logWriter.WriteAsync(new SystemLogWriteRequest
                {
                    ActorUserId = nurseUserId,
                    Module = "EXAMINATIONS",
                    Action = "CREATE_EXAMINATION",
                    TargetType = "Examination",
                    TargetId = visit.Code,
                    TargetLabel = $"Khám - {student.FullName}",
                    Description = $"Tạo phiếu khám cho học sinh {student.FullName}",
                    Status = "SUCCESS",
                    Metadata = new { }
                }, cancellationToken);

                return (true, 201, "Tạo phiếu khám thành công.", Array.Empty<(string, string, string)>(), response);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private async Task<(bool Success, string? ErrorMessage)> AssignUniqueVisitCodeAsync(HealthVisit visit, CancellationToken cancellationToken)
        {
            const int maxAttempts = 5;

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                visit.Code = await GenerateNextVisitCodeAsync(cancellationToken);
                _context.HealthVisits.Update(visit);

                try
                {
                    await _examinationRepository.SaveChangesAsync(cancellationToken);
                    return (true, null);
                }
                catch (DbUpdateException ex) when (IsDuplicateVisitCodeException(ex))
                {
                    if (attempt == maxAttempts - 1)
                    {
                        return (false, "Mã phiếu khám bị trùng sau nhiều lần thử.");
                    }
                }
            }

            return (false, "Không thể tạo mã phiếu khám duy nhất.");
        }

        private async Task<string> GenerateNextVisitCodeAsync(CancellationToken cancellationToken)
        {
            var codes = await _context.HealthVisits
                .AsNoTracking()
                .Where(x => x.Code.StartsWith("VIS"))
                .Select(x => x.Code)
                .ToListAsync(cancellationToken);

            var max = 0;
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var code in codes)
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    continue;
                }

                existing.Add(code);

                if (code.Length <= 3)
                {
                    continue;
                }

                if (int.TryParse(code[3..], out var value))
                {
                    if (value > max)
                    {
                        max = value;
                    }
                }
            }

            var next = max + 1;
            while (true)
            {
                var candidate = $"VIS{next:D3}";
                if (!existing.Contains(candidate))
                {
                    return candidate;
                }

                next += 1;
            }
        }

        private static bool IsDuplicateVisitCodeException(DbUpdateException exception)
        {
            var message = exception.InnerException?.Message ?? exception.Message;
            return message.Contains("IX_HealthVisits_Code", StringComparison.OrdinalIgnoreCase)
                || message.Contains("HealthVisits", StringComparison.OrdinalIgnoreCase)
                && message.Contains("Code", StringComparison.OrdinalIgnoreCase)
                && message.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
        }


    }
}
