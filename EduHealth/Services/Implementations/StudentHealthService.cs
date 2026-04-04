using EduHealth.Data;
using EduHealth.DTOs.Students.HealthProfile;
using EduHealth.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Services.Implementations
{
    public class StudentHealthService : IStudentHealthService
    {
        private static readonly HashSet<string> AllowedBloodTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "A", "B", "AB", "O", "UNKNOWN"
        };

        private readonly AppDbContext _context;

        public StudentHealthService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<StudentHealthProfileResponseDto?> GetHealthProfileAsync(int studentUserId, CancellationToken cancellationToken = default)
        {
            var student = await _context.Students
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Class)
                .Include(x => x.StudentAllergies)
                    .ThenInclude(sa => sa.AllergyType)
                .FirstOrDefaultAsync(x => x.UserId == studentUserId, cancellationToken);

            if (student is null)
            {
                return null;
            }

            // Note: current schema does not have separate health profile table.
            // Map what we have: height/weight from Student; other fields remain null.
            var allergies = student.StudentAllergies
                .OrderBy(x => x.RecordId)
                .Select(x => new StudentAllergyItemDto
                {
                    Id = $"SA{x.RecordId:D3}",
                    AllergyTypeId = $"ALG{x.AllergyId:D3}",
                    AllergyTypeName = x.AllergyType.AllergyName,
                    Note = x.Note
                })
                .ToList();

            return new StudentHealthProfileResponseDto
            {
                StudentId = student.Code,
                StudentCode = student.User.Username,
                FullName = student.FullName,
                ClassId = student.Class.Code,
                ClassName = student.Class.ClassName,
                HealthProfile = new HealthProfileDto
                {
                    HeightCm = student.CurrentHeight,
                    WeightKg = student.CurrentWeight,
                    BloodType = null,
                    EyeStatus = null,
                    ChronicNote = student.MedicalHistoryNotes,
                    GeneralHealthNote = null,
                    Allergies = allergies,
                    UpdatedBy = null,
                    UpdatedAt = null
                }
            };
        }

        public async Task<(bool Success, string Message, string? Field, StudentHealthProfileResponseDto? Data)> UpdateHealthProfileAsync(
            int nurseUserId,
            int studentUserId,
            UpdateStudentHealthProfileRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.HeightCm.HasValue && request.HeightCm.Value <= 0)
            {
                return (false, "Dữ liệu không hợp lệ.", "heightCm", null);
            }

            if (request.WeightKg.HasValue && request.WeightKg.Value <= 0)
            {
                return (false, "Dữ liệu không hợp lệ.", "weightKg", null);
            }

            if (!string.IsNullOrWhiteSpace(request.BloodType) && !AllowedBloodTypes.Contains(request.BloodType.Trim()))
            {
                return (false, "Dữ liệu không hợp lệ.", "bloodType", null);
            }

            // Current DB schema does not contain these fields. Reject if sent to avoid silent ignore.
            if (!string.IsNullOrWhiteSpace(request.EyeStatus))
            {
                return (false, "Dữ liệu không hợp lệ.", "eyeStatus", null);
            }

            if (!string.IsNullOrWhiteSpace(request.GeneralHealthNote))
            {
                return (false, "Dữ liệu không hợp lệ.", "generalHealthNote", null);
            }

            if (!string.IsNullOrWhiteSpace(request.ChronicNote))
            {
                return (false, "Dữ liệu không hợp lệ.", "chronicNote", null);
            }

            var student = await _context.Students
                .Include(x => x.User)
                .Include(x => x.Class)
                .Include(x => x.StudentAllergies)
                .FirstOrDefaultAsync(x => x.UserId == studentUserId, cancellationToken);

            if (student is null)
            {
                return (false, "Không tìm thấy học sinh.", "id", null);
            }

            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

            if (request.HeightCm.HasValue)
            {
                student.CurrentHeight = request.HeightCm.Value;
            }
            if (request.WeightKg.HasValue)
            {
                student.CurrentWeight = request.WeightKg.Value;
            }

            if (request.Allergies is not null)
            {
                var distinctAllergyIds = request.Allergies
                    .Where(x => x.AllergyId > 0)
                    .Select(x => x.AllergyId)
                    .Distinct()
                    .ToList();

                if (distinctAllergyIds.Count != request.Allergies.Count)
                {
                    return (false, "Dữ liệu không hợp lệ.", "allergies", null);
                }

                var existing = await _context.AllergyTypes
                    .Where(x => distinctAllergyIds.Contains(x.AllergyId))
                    .Select(x => x.AllergyId)
                    .ToListAsync(cancellationToken);

                if (existing.Count != distinctAllergyIds.Count)
                {
                    return (false, "Dữ liệu không hợp lệ.", "allergies", null);
                }

                // sync: remove all then add
                _context.StudentAllergies.RemoveRange(student.StudentAllergies);

                foreach (var a in request.Allergies)
                {
                    _context.StudentAllergies.Add(new Data.Entities.StudentAllergy
                    {
                        UserId = student.UserId,
                        AllergyId = a.AllergyId,
                        Note = string.IsNullOrWhiteSpace(a.Note) ? null : a.Note.Trim()
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            var data = await GetHealthProfileAsync(studentUserId, cancellationToken);
            return (true, "Cập nhật hồ sơ sức khỏe thành công.", null, data);
        }

        public async Task<(IReadOnlyList<StudentHealthHistoryItemDto> Items, int TotalItems, int TotalPages, int Page, int PageSize)?> GetHealthHistoryAsync(
            int studentUserId,
            StudentHealthHistoryQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var studentExists = await _context.Students.AsNoTracking().AnyAsync(x => x.UserId == studentUserId, cancellationToken);
            if (!studentExists)
            {
                return null;
            }

            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = query.PageSize <= 0 ? 10 : Math.Min(query.PageSize, 100);

            var q = _context.HealthVisits
                .AsNoTracking()
                .Include(x => x.Nurse)
                .Include(x => x.DiseaseType)
                .Include(x => x.VisitPrescriptions)
                    .ThenInclude(p => p.Medicine)
                .Where(x => x.StudentUserId == studentUserId);

            if (query.FromDate.HasValue)
            {
                q = q.Where(x => x.VisitDate >= query.FromDate.Value);
            }
            if (query.ToDate.HasValue)
            {
                q = q.Where(x => x.VisitDate <= query.ToDate.Value);
            }

            var total = await q.CountAsync(cancellationToken);

            var items = await q
                .OrderByDescending(x => x.VisitDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new StudentHealthHistoryItemDto
                {
                    VisitId = x.Code,
                    VisitDate = x.VisitDate,
                    Nurse = new NurseBriefDto
                    {
                        UserId = x.Nurse.Code,
                        FullName = x.Nurse.FullName
                    },
                    DiseaseType = x.DiseaseType == null ? null : new DiseaseBriefDto
                    {
                        Id = x.DiseaseType.Code,
                        Name = x.DiseaseType.DiseaseName
                    },
                    Symptoms = x.Symptoms,
                    Diagnosis = x.Diagnosis,
                    Treatment = x.Treatment,
                    Note = x.Note,
                    Prescriptions = x.VisitPrescriptions
                        .OrderBy(p => p.PrescriptionId)
                        .Select(p => new PrescriptionItemDto
                        {
                            PrescriptionId = $"VP{p.PrescriptionId:D3}",
                            MedicineId = p.Medicine.Code,
                            MedicineName = p.Medicine.Name,
                            Quantity = p.Quantity,
                            UsageInstruction = p.UsageIns
                        })
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            return (
                items,
                total,
                total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize),
                page,
                pageSize);
        }
    }
}
