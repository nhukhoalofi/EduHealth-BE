using EduHealth.Data;
using EduHealth.Data.Entities;
using EduHealth.DTOs.Vaccinations;
using EduHealth.Helpers;
using EduHealth.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Services.Implementations
{
    public class VaccinationService : IVaccinationService
    {
        private static readonly HashSet<string> AllowedTargetTypes = new(StringComparer.OrdinalIgnoreCase) { "CLASS", "STUDENT" };
        private static readonly HashSet<string> AllowedCampaignStatuses = new(StringComparer.OrdinalIgnoreCase) { "ACTIVE", "COMPLETED", "CANCELLED" };
        private static readonly HashSet<string> AllowedStudentVaccinationStatuses = new(StringComparer.OrdinalIgnoreCase) { "PENDING", "DONE", "POSTPONED", "CONTRAINDICATED", "ABSENT" };

        private readonly AppDbContext _context;
        private readonly ISystemLogWriter _logWriter;

        public VaccinationService(AppDbContext context, ISystemLogWriter logWriter)
        {
            _context = context;
            _logWriter = logWriter;
        }

        public async Task<(IReadOnlyList<VaccinationCampaignListItemDto> Items, int TotalItems, int TotalPages, int Page, int PageSize)> GetCampaignsAsync(
            VaccinationCampaignListQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);

            var q = _context.VaccinationCampaigns
                .AsNoTracking()
                .Include(x => x.TargetClasses)
                    .ThenInclude(t => t.Class)
                .Include(x => x.StudentVaccinations)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var k = query.Keyword.Trim();
                q = q.Where(x => x.Name.Contains(k) || x.VaccineName.Contains(k));
            }

            if (query.FromDate.HasValue)
            {
                q = q.Where(x => x.ScheduledDate >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                q = q.Where(x => x.ScheduledDate <= query.ToDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var s = query.Status.Trim().ToUpperInvariant();
                q = q.Where(x => x.Status == s);
            }

            if (!string.IsNullOrWhiteSpace(query.ClassId))
            {
                var classCode = query.ClassId.Trim();
                q = q.Where(x => x.TargetClasses.Any(t => t.Class.Code == classCode));
            }

            var total = await q.CountAsync(cancellationToken);

            var items = await q
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new VaccinationCampaignListItemDto
                {
                    Id = x.Code,
                    Name = x.Name,
                    VaccineName = x.VaccineName,
                    DoseNumber = x.DoseNumber,
                    ScheduledDate = x.ScheduledDate,
                    TargetType = x.TargetType,
                    Status = x.Status,
                    Statistics = BuildStats(x.StudentVaccinations)
                })
                .ToListAsync(cancellationToken);

            return (items, total, total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize), page, pageSize);
        }

        public async Task<(bool Success, int StatusCode, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, CreateVaccinationCampaignResponseDto? Data)> CreateCampaignAsync(
            int createdByUserId,
            CreateVaccinationCampaignRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<(string Field, string Code, string Message)>();

            if (string.IsNullOrWhiteSpace(request.Name))
                errors.Add(("name", "REQUIRED", "name là bắt buộc."));
            if (string.IsNullOrWhiteSpace(request.VaccineName))
                errors.Add(("vaccineName", "REQUIRED", "vaccineName là bắt buộc."));
            if (request.DoseNumber <= 0)
                errors.Add(("doseNumber", "INVALID_DOSE", "doseNumber phải lớn hơn 0."));

            if (!AllowedTargetTypes.Contains(request.TargetType?.Trim() ?? string.Empty))
                errors.Add(("targetType", "INVALID_TARGET_TYPE", "targetType chỉ nhận CLASS hoặc STUDENT."));

            if (errors.Count > 0)
                return (false, 400, "Dữ liệu không hợp lệ.", errors, null);

            var targetType = request.TargetType.Trim().ToUpperInvariant();

            if (targetType == "CLASS")
            {
                if (request.TargetClassIds is null || request.TargetClassIds.Count == 0)
                {
                    return (false, 400, "Dữ liệu không hợp lệ.", new[] { ("targetClassIds", "REQUIRED", "targetClassIds là bắt buộc khi targetType=CLASS.") }, null);
                }
            }
            else
            {
                if (request.TargetStudentIds is null || request.TargetStudentIds.Count == 0)
                {
                    return (false, 400, "Dữ liệu không hợp lệ.", new[] { ("targetStudentIds", "REQUIRED", "targetStudentIds là bắt buộc khi targetType=STUDENT.") }, null);
                }
            }

            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

            var vaccination = await _context.Vaccinations.FirstOrDefaultAsync(x => x.Name == request.VaccineName.Trim(), cancellationToken);
            if (vaccination is null)
            {
                vaccination = new Vaccination
                {
                    Name = request.VaccineName.Trim(),
                    Description = null,
                    CreatedAt = VietnamTimeHelper.Now
                };
                _context.Vaccinations.Add(vaccination);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var campaign = new VaccinationCampaign
            {
                Code = "VAC_TMP",
                Name = request.Name.Trim(),
                VaccineName = request.VaccineName.Trim(),
                DoseNumber = request.DoseNumber,
                ScheduledDate = request.ScheduledDate,
                TargetType = targetType,
                Status = "ACTIVE",
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                CreatedByUserId = createdByUserId,
                CreatedAt = VietnamTimeHelper.Now
            };

            _context.VaccinationCampaigns.Add(campaign);
            await _context.SaveChangesAsync(cancellationToken);

            campaign.Code = $"VAC{campaign.CampaignId:D3}";
            _context.VaccinationCampaigns.Update(campaign);
            await _context.SaveChangesAsync(cancellationToken);

            var targetUserIds = new List<int>();
            var targetClassCodes = new List<string>();

            if (targetType == "CLASS")
            {
                var requestedClassIds = request.TargetClassIds!
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var allClasses = await _context.SchoolClasses.ToListAsync(cancellationToken);
                var classByCode = allClasses.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
                var classById = allClasses.ToDictionary(x => x.ClassId);
                var classes = new List<SchoolClass>();
                var missingClassIds = new List<string>();

                foreach (var requestedClassId in requestedClassIds)
                {
                    if (classByCode.TryGetValue(requestedClassId, out var classByRequestedCode))
                    {
                        classes.Add(classByRequestedCode);
                        continue;
                    }

                    if (int.TryParse(requestedClassId, out var numericClassId) &&
                        classById.TryGetValue(numericClassId, out var classByNumericId))
                    {
                        classes.Add(classByNumericId);
                        continue;
                    }

                    if (requestedClassId.StartsWith("CLS", StringComparison.OrdinalIgnoreCase) &&
                        int.TryParse(requestedClassId[3..], out var legacyClassId) &&
                        classById.TryGetValue(legacyClassId, out var classByLegacyId))
                    {
                        classes.Add(classByLegacyId);
                        continue;
                    }

                    missingClassIds.Add(requestedClassId);
                }

                classes = classes
                    .GroupBy(x => x.ClassId)
                    .Select(x => x.First())
                    .ToList();

                if (missingClassIds.Count > 0)
                {
                    return (false, 400, "Dữ liệu không hợp lệ.", new[] { ("targetClassIds", "CLASS_NOT_FOUND", "Có lớp không tồn tại." ) }, null);
                }

                targetClassCodes = classes.Select(x => x.Code).ToList();

                foreach (var c in classes)
                {
                    _context.VaccinationCampaignTargetClasses.Add(new VaccinationCampaignTargetClass
                    {
                        CampaignId = campaign.CampaignId,
                        ClassId = c.ClassId
                    });
                }

                targetUserIds = await _context.Students
                    .AsNoTracking()
                    .Where(s => classes.Select(c => c.ClassId).Contains(s.ClassId))
                    .Select(s => s.UserId)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                targetUserIds = request.TargetStudentIds!.Distinct().ToList();
                var existingStudents = await _context.Students.AsNoTracking().Where(x => targetUserIds.Contains(x.UserId)).Select(x => x.UserId).ToListAsync(cancellationToken);
                if (existingStudents.Count != targetUserIds.Count)
                {
                    return (false, 400, "Dữ liệu không hợp lệ.", new[] { ("targetStudentIds", "STUDENT_NOT_FOUND", "Có học sinh không tồn tại." ) }, null);
                }
            }

            // generate student vaccination records
            var now = VietnamTimeHelper.Now;
            foreach (var uid in targetUserIds.Distinct())
            {
                _context.StudentVaccinations.Add(new StudentVaccination
                {
                    UserId = uid,
                    CampaignId = campaign.CampaignId,
                    VaccinationId = vaccination.VaccinationId,
                    Status = "PENDING",
                    UpdatedAt = now
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            await _logWriter.WriteAsync(new SystemLogWriteRequest
            {
                ActorUserId = createdByUserId,
                Module = "VACCINATIONS",
                Action = "CREATE_CAMPAIGN",
                TargetType = "VaccinationCampaign",
                TargetId = campaign.Code,
                TargetLabel = campaign.Name,
                Description = $"Tạo đợt tiêm {campaign.Name}",
                Status = "SUCCESS",
                Metadata = new { }
            }, cancellationToken);

            return (true, 201, "Tạo đợt tiêm thành công.", Array.Empty<(string, string, string)>(), new CreateVaccinationCampaignResponseDto
            {
                Id = campaign.Code,
                Name = campaign.Name,
                VaccineName = campaign.VaccineName,
                DoseNumber = campaign.DoseNumber,
                ScheduledDate = campaign.ScheduledDate,
                TargetType = campaign.TargetType,
                TargetClassIds = targetClassCodes,
                GeneratedStudentRecords = targetUserIds.Distinct().Count(),
                Status = campaign.Status,
                CreatedAt = campaign.CreatedAt
            });
        }

        public async Task<VaccinationCampaignDetailDto?> GetCampaignDetailAsync(string id, CancellationToken cancellationToken = default)
        {
            id = id.Trim();

            var campaign = await _context.VaccinationCampaigns
                .AsNoTracking()
                .Include(x => x.TargetClasses)
                    .ThenInclude(t => t.Class)
                .Include(x => x.StudentVaccinations)
                .FirstOrDefaultAsync(x => x.Code == id, cancellationToken);

            if (campaign is null) return null;

            return new VaccinationCampaignDetailDto
            {
                Id = campaign.Code,
                Name = campaign.Name,
                VaccineName = campaign.VaccineName,
                DoseNumber = campaign.DoseNumber,
                ScheduledDate = campaign.ScheduledDate,
                TargetType = campaign.TargetType,
                TargetClassIds = campaign.TargetClasses.Select(x => x.Class.Code).OrderBy(x => x).ToList(),
                Note = campaign.Note,
                Status = campaign.Status,
                Statistics = BuildStats(campaign.StudentVaccinations),
                CreatedAt = campaign.CreatedAt
            };
        }

        public async Task<(IReadOnlyList<CampaignStudentItemDto> Items, int TotalItems, int TotalPages, int Page, int PageSize)?> GetCampaignStudentsAsync(
            string campaignId,
            CampaignStudentListQueryDto query,
            CancellationToken cancellationToken = default)
        {
            campaignId = campaignId.Trim();
            var campaign = await _context.VaccinationCampaigns.AsNoTracking().FirstOrDefaultAsync(x => x.Code == campaignId, cancellationToken);
            if (campaign is null) return null;

            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);

            var q = _context.StudentVaccinations
                .AsNoTracking()
                .Include(x => x.Student)
                    .ThenInclude(s => s.User)
                .Include(x => x.Student)
                    .ThenInclude(s => s.Class)
                .Where(x => x.CampaignId == campaign.CampaignId);

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var st = query.Status.Trim().ToUpperInvariant();
                q = q.Where(x => x.Status == st);
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var k = query.Keyword.Trim();
                q = q.Where(x => x.Student.FullName.Contains(k) || x.Student.User.Username.Contains(k) || x.Student.Class.ClassName.Contains(k));
            }

            var total = await q.CountAsync(cancellationToken);

            var items = await q
                .OrderBy(x => x.Student.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CampaignStudentItemDto
                {
                    StudentVaccinationId = $"SV{x.RecordId:D3}",
                    Student = new CampaignStudentBriefDto
                    {
                        StudentId = x.Student.Code,
                        StudentCode = x.Student.User.Username,
                        FullName = x.Student.FullName,
                        ClassId = x.Student.Class.Code,
                        ClassName = x.Student.Class.ClassName
                    },
                    Status = x.Status,
                    VaccinatedAt = x.VaccinatedAt,
                    LotNumber = x.LotNumber,
                    Note = x.Note
                })
                .ToListAsync(cancellationToken);

            return (items, total, total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize), page, pageSize);
        }

        public async Task<(bool Success, int StatusCode, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, UpdateStudentVaccinationResponseDto? Data)> UpdateStudentVaccinationAsync(
            string studentVaccinationId,
            UpdateStudentVaccinationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            studentVaccinationId = studentVaccinationId.Trim();
            if (!studentVaccinationId.StartsWith("SV", StringComparison.OrdinalIgnoreCase) || !int.TryParse(studentVaccinationId[2..], out var recordId))
            {
                return (false, 400, "Dữ liệu không hợp lệ.", new[] { ("id", "INVALID_ID", "id không hợp lệ.") }, null);
            }

            if (string.IsNullOrWhiteSpace(request.Status) || !AllowedStudentVaccinationStatuses.Contains(request.Status.Trim()))
            {
                return (false, 400, "Dữ liệu không hợp lệ.", new[] { ("status", "INVALID_VACCINATION_STATUS", "Trạng thái tiêm không hợp lệ.") }, null);
            }

            var entity = await _context.StudentVaccinations
                .Include(x => x.Campaign)
                .FirstOrDefaultAsync(x => x.RecordId == recordId, cancellationToken);

            if (entity is null)
            {
                return (false, 404, "Không tìm thấy bản ghi tiêm.", new[] { ("id", "STUDENT_VACCINATION_NOT_FOUND", "Không tồn tại bản ghi tiêm với id đã cung cấp.") }, null);
            }

            var status = request.Status.Trim().ToUpperInvariant();

            if (entity.Status == "DONE")
            {
                return (false, 409, "Báº£n ghi Ä‘Ã£ hoÃ n táº¥t, khÃ´ng thá»ƒ cáº­p nháº­t.", new[] { ("status", "VACCINATION_ALREADY_DONE", "Báº£n ghi Ä‘Ã£ tiÃªm khÃ´ng thá»ƒ cáº­p nháº­t.") }, null);
            }

            if (status == "DONE")
            {
                if (!request.VaccinatedAt.HasValue)
                {
                    return (false, 400, "Dá»¯ liá»‡u khÃ´ng há»£p lá»‡.", new[] { ("vaccinatedAt", "REQUIRED", "vaccinatedAt lÃ  báº¯t buá»™c khi tráº¡ng thÃ¡i lÃ  DONE.") }, null);
                }

                if (request.VaccinatedAt.Value > DateOnly.FromDateTime(VietnamTimeHelper.Now))
                {
                    return (false, 400, "Dá»¯ liá»‡u khÃ´ng há»£p lá»‡.", new[] { ("vaccinatedAt", "FUTURE_DATE", "NgÃ y tiÃªm thá»±c táº¿ khÃ´ng Ä‘Æ°á»£c lá»›n hÆ¡n ngÃ y hiá»‡n táº¡i.") }, null);
                }
            }

            entity.Status = status;
            entity.VaccinatedAt = request.VaccinatedAt;
            entity.LotNumber = string.IsNullOrWhiteSpace(request.LotNumber) ? null : request.LotNumber.Trim();
            entity.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
            entity.UpdatedAt = VietnamTimeHelper.Now;

            _context.StudentVaccinations.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);

            await _logWriter.WriteAsync(new SystemLogWriteRequest
            {
                ActorUserId = null,
                Module = "VACCINATIONS",
                Action = "UPDATE_STUDENT_VACCINATION",
                TargetType = "StudentVaccination",
                TargetId = $"SV{entity.RecordId:D3}",
                TargetLabel = $"Tiêm - {entity.Campaign.Name}",
                Description = "Cập nhật trạng thái tiêm chủng",
                Status = "SUCCESS",
                Metadata = new { studentId = entity.UserId, campaignId = entity.CampaignId, status = status }
            }, cancellationToken);

            return (true, 200, "Cập nhật trạng thái tiêm thành công.", Array.Empty<(string, string, string)>(), new UpdateStudentVaccinationResponseDto
            {
                StudentVaccinationId = $"SV{entity.RecordId:D3}",
                CampaignId = entity.Campaign.Code,
                StudentId = $"STD{entity.UserId:D3}",
                Status = entity.Status,
                VaccinatedAt = entity.VaccinatedAt,
                LotNumber = entity.LotNumber,
                Note = entity.Note,
                UpdatedAt = entity.UpdatedAt
            });
        }

        public async Task<(IReadOnlyList<PendingVaccinationItemDto> Items, int TotalItems, int TotalPages, int Page, int PageSize)> GetPendingAsync(
            PendingVaccinationsQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);

            var q = _context.StudentVaccinations
                .AsNoTracking()
                .Include(x => x.Campaign)
                .Include(x => x.Student)
                    .ThenInclude(s => s.User)
                .Include(x => x.Student)
                    .ThenInclude(s => s.Class)
                .Where(x => x.Status != "DONE");

            if (!string.IsNullOrWhiteSpace(query.CampaignId))
            {
                var cid = query.CampaignId.Trim();
                q = q.Where(x => x.Campaign.Code == cid);
            }

            if (!string.IsNullOrWhiteSpace(query.ClassId))
            {
                var classCode = query.ClassId.Trim();
                q = q.Where(x => x.Student.Class.Code == classCode);
            }

            var total = await q.CountAsync(cancellationToken);

            var items = await q
                .OrderByDescending(x => x.Campaign.ScheduledDate)
                .ThenBy(x => x.Student.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new PendingVaccinationItemDto
                {
                    StudentVaccinationId = $"SV{x.RecordId:D3}",
                    CampaignId = x.Campaign.Code,
                    CampaignName = x.Campaign.Name,
                    Student = new CampaignStudentBriefDto
                    {
                        StudentId = x.Student.Code,
                        StudentCode = x.Student.User.Username,
                        FullName = x.Student.FullName,
                        ClassId = x.Student.Class.Code,
                        ClassName = x.Student.Class.ClassName
                    },
                    Status = x.Status,
                    ScheduledDate = x.Campaign.ScheduledDate
                })
                .ToListAsync(cancellationToken);

            return (items, total, total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize), page, pageSize);
        }

        public async Task<IReadOnlyList<StudentVaccinationHistoryItemDto>?> GetStudentVaccinationHistoryAsync(int studentUserId, CancellationToken cancellationToken = default)
        {
            var studentExists = await _context.Students.AsNoTracking().AnyAsync(x => x.UserId == studentUserId, cancellationToken);
            if (!studentExists) return null;

            var items = await _context.StudentVaccinations
                .AsNoTracking()
                .Include(x => x.Campaign)
                .Where(x => x.UserId == studentUserId)
                .OrderByDescending(x => x.Campaign.ScheduledDate)
                .Select(x => new StudentVaccinationHistoryItemDto
                {
                    StudentVaccinationId = $"SV{x.RecordId:D3}",
                    CampaignId = x.Campaign.Code,
                    CampaignName = x.Campaign.Name,
                    VaccineName = x.Campaign.VaccineName,
                    DoseNumber = x.Campaign.DoseNumber,
                    ScheduledDate = x.Campaign.ScheduledDate,
                    Status = x.Status,
                    VaccinatedAt = x.VaccinatedAt,
                    LotNumber = x.LotNumber,
                    Note = x.Note
                })
                .ToListAsync(cancellationToken);

            return items;
        }

        private static CampaignStatisticsDto BuildStats(ICollection<StudentVaccination> records)
        {
            return new CampaignStatisticsDto
            {
                TotalStudents = records.Count,
                DoneCount = records.Count(x => x.Status == "DONE"),
                PendingCount = records.Count(x => x.Status == "PENDING"),
                PostponedCount = records.Count(x => x.Status == "POSTPONED"),
                ContraindicatedCount = records.Count(x => x.Status == "CONTRAINDICATED"),
                AbsentCount = records.Count(x => x.Status == "ABSENT")
            };
        }
    }
}
