using EduHealth.Data;
using EduHealth.Data.Entities;
using EduHealth.DTOs.Examinations;
using EduHealth.Repositories.Interfaces;
using EduHealth.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Services.Implementations
{
    public class ExaminationService : IExaminationService
    {
        private readonly AppDbContext _context;
        private readonly IExaminationRepository _examinationRepository;

        public ExaminationService(AppDbContext context, IExaminationRepository examinationRepository)
        {
            _context = context;
            _examinationRepository = examinationRepository;
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

            DiseaseType? disease = null;
            if (!string.IsNullOrWhiteSpace(request.DiseaseTypeId))
            {
                disease = await _examinationRepository.GetDiseaseTypeByCodeAsync(request.DiseaseTypeId.Trim(), cancellationToken);
                if (disease is null)
                {
                    return (false, 404, "Không tìm thấy dữ liệu liên quan.", new[] { ("diseaseTypeId", "DISEASE_TYPE_NOT_FOUND", "diseaseTypeId không tồn tại.") }, null);
                }
            }

            var prescriptions = request.Prescriptions?.ToList() ?? new List<CreateExaminationPrescriptionItemDto>();

            // Transaction
            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
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

                visit.Code = $"VIS{visit.VisitId:D3}";
                _context.HealthVisits.Update(visit);
                await _examinationRepository.SaveChangesAsync(cancellationToken);

                var createdPrescriptions = new List<VisitPrescription>();
                var inventoryMovements = new List<MedicineStockLog>();
                var meds = new Dictionary<string, Medicine>(StringComparer.OrdinalIgnoreCase);

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

                    for (var i = 0; i < prescriptions.Count; i++)
                    {
                        var item = prescriptions[i];
                        var med = meds[item.MedicineId];

                        if (item.Quantity > med.StockQuantity)
                        {
                            return (false, 400, "Dữ liệu không hợp lệ.", new[] { ($"prescriptions[{i}].quantity", "INSUFFICIENT_STOCK", "Số lượng thuốc trong kho không đủ.") }, null);
                        }
                    }

                    foreach (var item in prescriptions)
                    {
                        var med = meds[item.MedicineId];
                        var stockBefore = med.StockQuantity;
                        med.StockQuantity -= item.Quantity;
                        med.UpdatedAt = DateTime.UtcNow;

                        createdPrescriptions.Add(new VisitPrescription
                        {
                            VisitId = visit.VisitId,
                            MedicineId = med.MedicineId,
                            Quantity = item.Quantity,
                            UsageIns = item.UsageInstruction
                        });

                        inventoryMovements.Add(new MedicineStockLog
                        {
                            MedicineId = med.MedicineId,
                            UserId = nurseUserId,
                            Quantity = item.Quantity,
                            StockBefore = stockBefore,
                            StockAfter = med.StockQuantity,
                            Type = "DISPENSE",
                            Reason = null,
                            ExpiryDate = null,
                            BatchNumber = null,
                            Note = null,
                            VisitId = visit.VisitId,
                            CreatedAt = DateTime.UtcNow
                        });
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
                var nurse = await _context.Users.AsNoTracking().FirstAsync(x => x.UserId == nurseUserId, cancellationToken);

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
                            Type = m.Type,
                            Quantity = m.Quantity
                        };
                    }).ToList(),
                    CreatedAt = DateTime.UtcNow
                };

                return (true, 201, "Tạo phiếu khám thành công.", Array.Empty<(string, string, string)>(), response);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        }


    }
}
