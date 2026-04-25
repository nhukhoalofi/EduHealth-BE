using EduHealth.Data;
using EduHealth.DTOs.Students.HealthProfile;
using EduHealth.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Services.Implementations
{
    public class StudentHealthService : IStudentHealthService
    {
        private sealed record ParsedHealthNotes(
            string? BloodType,
            string? EyeStatus,
            string? ChronicNote,
            string? GeneralHealthNote,
            List<string> LegacyLines);

        private static readonly HashSet<string> AllowedBloodTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "A", "B", "AB", "O", "UNKNOWN"
        };

        private readonly AppDbContext _context;
        private readonly ISystemLogWriter _logWriter;

        public StudentHealthService(AppDbContext context, ISystemLogWriter logWriter)
        {
            _context = context;
            _logWriter = logWriter;
        }

        public async Task<IReadOnlyList<AllergyTypeLookupItemDto>> GetAllergyTypesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.AllergyTypes
                .AsNoTracking()
                .OrderBy(x => x.AllergyName)
                .Select(x => new AllergyTypeLookupItemDto
                {
                    AllergyId = x.AllergyId,
                    AllergyTypeId = $"ALG{x.AllergyId:D3}",
                    AllergyTypeName = x.AllergyName,
                    Severity = x.Severity
                })
                .ToListAsync(cancellationToken);
        }

        private static string? NormalizeNullableText(string? value)
        {
            if (value is null)
            {
                return null;
            }

            var normalized = value.Trim();
            return normalized.Length == 0 ? null : normalized;
        }

        private static bool TryParsePrefixedLine(string line, string prefix, out string? value)
        {
            if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                value = NormalizeNullableText(line[prefix.Length..]);
                return true;
            }

            value = null;
            return false;
        }

        private static ParsedHealthNotes ParseHealthNotes(string? medicalHistoryNotes)
        {
            var legacyLines = new List<string>();
            if (string.IsNullOrWhiteSpace(medicalHistoryNotes))
            {
                return new ParsedHealthNotes(null, null, null, null, legacyLines);
            }

            string? bloodType = null;
            string? eyeStatus = null;
            string? chronicNote = null;
            string? generalHealthNote = null;

            var lines = medicalHistoryNotes
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var line in lines)
            {
                if (TryParsePrefixedLine(line, "Nhóm máu:", out var parsedBlood)
                    || TryParsePrefixedLine(line, "Nhom mau:", out parsedBlood)
                    || TryParsePrefixedLine(line, "Blood type:", out parsedBlood)
                    || TryParsePrefixedLine(line, "BloodType:", out parsedBlood))
                {
                    bloodType = parsedBlood;
                    continue;
                }

                if (TryParsePrefixedLine(line, "Tình trạng mắt:", out var parsedEye)
                    || TryParsePrefixedLine(line, "Tinh trang mat:", out parsedEye))
                {
                    eyeStatus = parsedEye;
                    continue;
                }

                if (TryParsePrefixedLine(line, "Ghi chú bệnh nền:", out var parsedChronic)
                    || TryParsePrefixedLine(line, "Ghi chu benh nen:", out parsedChronic)
                    || TryParsePrefixedLine(line, "Ghi chú bệnh mãn tính:", out parsedChronic)
                    || TryParsePrefixedLine(line, "Ghi chu benh man tinh:", out parsedChronic))
                {
                    chronicNote = parsedChronic;
                    continue;
                }

                if (TryParsePrefixedLine(line, "Ghi chú sức khỏe chung:", out var parsedGeneral)
                    || TryParsePrefixedLine(line, "Ghi chu suc khoe chung:", out parsedGeneral)
                    || TryParsePrefixedLine(line, "General health note:", out parsedGeneral))
                {
                    generalHealthNote = parsedGeneral;
                    continue;
                }

                legacyLines.Add(line);
            }

            return new ParsedHealthNotes(bloodType, eyeStatus, chronicNote, generalHealthNote, legacyLines);
        }

        private static string? BuildMedicalHistoryNotes(
            string? bloodType,
            string? eyeStatus,
            string? chronicNote,
            string? generalHealthNote,
            IEnumerable<string> legacyLines)
        {
            var lines = new List<string>();

            if (!string.IsNullOrWhiteSpace(bloodType))
            {
                lines.Add($"Nhóm máu: {bloodType}");
            }

            if (!string.IsNullOrWhiteSpace(eyeStatus))
            {
                lines.Add($"Tình trạng mắt: {eyeStatus}");
            }

            if (!string.IsNullOrWhiteSpace(chronicNote))
            {
                lines.Add($"Ghi chú bệnh nền: {chronicNote}");
            }

            if (!string.IsNullOrWhiteSpace(generalHealthNote))
            {
                lines.Add($"Ghi chú sức khỏe chung: {generalHealthNote}");
            }

            foreach (var legacy in legacyLines)
            {
                var normalized = NormalizeNullableText(legacy);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    lines.Add(normalized);
                }
            }

            return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);
        }

        private static int? ParseAllergyId(UpdateStudentAllergyItemDto? allergyItem)
        {
            if (allergyItem is null)
            {
                return null;
            }

            if (allergyItem.AllergyId.HasValue && allergyItem.AllergyId.Value > 0)
            {
                return allergyItem.AllergyId.Value;
            }

            var rawTypeId = NormalizeNullableText(allergyItem.AllergyTypeId);
            if (rawTypeId is null)
            {
                return null;
            }

            var digits = new string(rawTypeId.Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(digits))
            {
                return null;
            }

            if (!int.TryParse(digits, out var parsed) || parsed <= 0)
            {
                return null;
            }

            return parsed;
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

            var parsedNotes = ParseHealthNotes(student.MedicalHistoryNotes);
            var legacyNote = parsedNotes.LegacyLines.Count == 0
                ? null
                : string.Join(Environment.NewLine, parsedNotes.LegacyLines);

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
                    BloodType = parsedNotes.BloodType,
                    EyeStatus = parsedNotes.EyeStatus,
                    ChronicNote = parsedNotes.ChronicNote ?? legacyNote,
                    GeneralHealthNote = parsedNotes.GeneralHealthNote,
                    Allergies = allergies,
                    UpdatedBy = null,
                    UpdatedAt = student.User.UpdatedAt
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

            var normalizedBloodType = NormalizeNullableText(request.BloodType);
            if (normalizedBloodType is not null)
            {
                normalizedBloodType = normalizedBloodType.ToUpperInvariant();
                if (!AllowedBloodTypes.Contains(normalizedBloodType))
                {
                    return (false, "Dữ liệu không hợp lệ.", "bloodType", null);
                }
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

            var parsedNotes = ParseHealthNotes(student.MedicalHistoryNotes);

            var bloodType = parsedNotes.BloodType;
            var eyeStatus = parsedNotes.EyeStatus;
            var chronicNote = parsedNotes.ChronicNote;
            var generalHealthNote = parsedNotes.GeneralHealthNote;
            var legacyLines = new List<string>(parsedNotes.LegacyLines);

            if (request.HeightCm.HasValue)
            {
                student.CurrentHeight = request.HeightCm.Value;
            }
            if (request.WeightKg.HasValue)
            {
                student.CurrentWeight = request.WeightKg.Value;
            }

            if (request.BloodType is not null)
            {
                bloodType = normalizedBloodType;
            }

            if (request.EyeStatus is not null)
            {
                eyeStatus = NormalizeNullableText(request.EyeStatus);
            }

            if (request.ChronicNote is not null)
            {
                chronicNote = NormalizeNullableText(request.ChronicNote);
            }

            if (request.GeneralHealthNote is not null)
            {
                generalHealthNote = NormalizeNullableText(request.GeneralHealthNote);
            }

            student.MedicalHistoryNotes = BuildMedicalHistoryNotes(
                bloodType,
                eyeStatus,
                chronicNote,
                generalHealthNote,
                legacyLines);
            student.User.UpdatedAt = DateTime.UtcNow;
            _context.Entry(student.User).Property(x => x.UpdatedAt).IsModified = true;

            if (request.Allergies is not null)
            {
                var normalizedAllergyIds = request.Allergies
                    .Select(ParseAllergyId)
                    .ToList();

                if (normalizedAllergyIds.Any(x => !x.HasValue))
                {
                    return (false, "Dữ liệu không hợp lệ.", "allergies", null);
                }

                var allergyIds = normalizedAllergyIds
                    .Select(x => x!.Value)
                    .ToList();

                var distinctAllergyIds = allergyIds
                    .Distinct()
                    .ToList();

                if (distinctAllergyIds.Count != allergyIds.Count)
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

                for (var index = 0; index < request.Allergies.Count; index++)
                {
                    var item = request.Allergies[index];
                    _context.StudentAllergies.Add(new Data.Entities.StudentAllergy
                    {
                        UserId = student.UserId,
                        AllergyId = allergyIds[index],
                        Note = string.IsNullOrWhiteSpace(item.Note) ? null : item.Note.Trim()
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            await _logWriter.WriteAsync(new SystemLogWriteRequest
            {
                ActorUserId = null,
                Module = "HEALTH_PROFILES",
                Action = "UPDATE_HEALTH_PROFILE",
                TargetType = "HealthProfile",
                TargetId = $"STD{student.UserId:D3}",
                TargetLabel = student.FullName,
                Description = "Cập nhật hồ sơ sức khỏe học sinh",
                Status = "SUCCESS",
                Metadata = new { }
            }, cancellationToken);

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
