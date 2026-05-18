using EduHealth.Data;
using EduHealth.Data.Entities;
using EduHealth.DTOs.Diseases;
using EduHealth.Helpers;
using EduHealth.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Services.Implementations
{
    public sealed class DiseaseService : IDiseaseService
    {
        private readonly AppDbContext _context;
        private readonly ISystemLogWriter _logWriter;

        public DiseaseService(AppDbContext context, ISystemLogWriter logWriter)
        {
            _context = context;
            _logWriter = logWriter;
        }

        public async Task<IReadOnlyList<DiseaseListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.DiseaseTypes
                .AsNoTracking()
                .OrderBy(x => x.DiseaseName)
                .Select(x => new DiseaseListItemDto
                {
                    Id = x.DiseaseId,
                    Name = x.DiseaseName,
                    Description = x.Description
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<(bool Success, int? StatusCode, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, DiseaseDetailDto? Data)> CreateAsync(
            CreateDiseaseRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<(string Field, string Code, string Message)>();

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                errors.Add(("name", "REQUIRED", "name bắt buộc."));
            }

            if (errors.Count > 0)
            {
                return (false, 400, "Dữ liệu không hợp lệ.", errors, null);
            }

            var name = request.Name.Trim();
            var normalizedName = name.ToLowerInvariant();

            var nameExists = await _context.DiseaseTypes
                .AsNoTracking()
                .AnyAsync(x => x.DiseaseName.ToLower() == normalizedName, cancellationToken);

            if (nameExists)
            {
                return (false, 409, "Loại bệnh đã tồn tại.", new[] { ("name", "DISEASE_ALREADY_EXISTS", "Tên loại bệnh đã tồn tại trong hệ thống.") }, null);
            }

            var now = VietnamTimeHelper.Now;

            var disease = new DiseaseType
            {
                Code = "DIS_TMP",
                DiseaseName = name,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                IsContagious = false,
                StandardTreatment = null
            };

            _context.DiseaseTypes.Add(disease);
            await _context.SaveChangesAsync(cancellationToken);

            disease.Code = $"DIS{disease.DiseaseId:D3}";
            _context.DiseaseTypes.Update(disease);
            await _context.SaveChangesAsync(cancellationToken);

            await _logWriter.WriteAsync(new SystemLogWriteRequest
            {
                ActorUserId = null,
                Module = "DISEASES",
                Action = "CREATE_DISEASE",
                TargetType = "DiseaseType",
                TargetId = disease.Code,
                TargetLabel = disease.DiseaseName,
                Description = "Tạo loại bệnh mới",
                Status = "SUCCESS",
                Metadata = new { }
            }, cancellationToken);

            return (true, 201, "Tạo loại bệnh thành công.", Array.Empty<(string, string, string)>(), new DiseaseDetailDto
            {
                Id = disease.DiseaseId,
                Name = disease.DiseaseName,
                Description = disease.Description
            });
        }
    }
}
