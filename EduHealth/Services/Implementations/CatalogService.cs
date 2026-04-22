using EduHealth.Data;
using EduHealth.DTOs.Catalogs;
using EduHealth.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Services.Implementations
{
    public sealed class CatalogService : ICatalogService
    {
        private static readonly IReadOnlyList<CatalogGroupDto> Groups =
        [
            new CatalogGroupDto { Key = "vaccines", Label = "Vắc xin" },
            new CatalogGroupDto { Key = "diseases", Label = "Bệnh lý" },
            new CatalogGroupDto { Key = "allergies", Label = "Dị ứng" }
        ];

        private static readonly string[] AllowedGroups = ["vaccines", "diseases", "allergies"];
        private static readonly string[] AllowedStatuses = ["ACTIVE", "INACTIVE", "UNSTANDARDIZED"];

        private readonly AppDbContext _context;

        public CatalogService(AppDbContext context)
        {
            _context = context;
        }

        public IReadOnlyList<CatalogGroupDto> GetGroups() => Groups;

        public async Task<(IReadOnlyList<CatalogItemDto> Items, int TotalItems, int TotalPages, int Page, int PageSize)> GetItemsAsync(CatalogItemsQueryDto query, CancellationToken cancellationToken)
        {
            var group = (query.Group ?? string.Empty).Trim().ToLowerInvariant();
            if (!AllowedGroups.Contains(group))
            {
                return (Array.Empty<CatalogItemDto>(), 0, 0, NormalizePage(query.Page), NormalizePageSize(query.PageSize));
            }

            var keyword = query.Keyword?.Trim().ToLowerInvariant();
            var status = query.Status?.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(status) && !AllowedStatuses.Contains(status))
            {
                status = null;
            }

            var page = NormalizePage(query.Page);
            var pageSize = NormalizePageSize(query.PageSize);

            // Since all underlying data only represent "ACTIVE" records
            if (!string.IsNullOrWhiteSpace(status) && status != "ACTIVE")
            {
                return (Array.Empty<CatalogItemDto>(), 0, 0, page, pageSize);
            }

            if (group == "vaccines")
            {
                var q = _context.Vaccinations.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    if ((keyword.StartsWith("vac-") || keyword.StartsWith("vac")) && TryParseKeywordId(keyword, "vac-", "vac", out int id))
                    {
                        q = q.Where(x => x.VaccinationId == id || x.Name.ToLower().Contains(keyword));
                    }
                    else
                    {
                        q = q.Where(x => x.Name.ToLower().Contains(keyword) || x.VaccinationId.ToString().Contains(keyword));
                    }
                }

                var totalItems = await q.CountAsync(cancellationToken);
                var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

                var dbItems = await q.OrderBy(x => x.VaccinationId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                var items = dbItems.Select(x => new CatalogItemDto
                {
                    Id = $"VAC{x.VaccinationId:D3}",
                    Group = "vaccines",
                    Code = $"VAC-{x.VaccinationId:D3}",
                    Name = x.Name,
                    Description = null,
                    Status = "ACTIVE",
                    CreatedAt = null,
                    UpdatedAt = null
                }).ToList();

                return (items, totalItems, totalPages, page, pageSize);
            }
            else if (group == "diseases")
            {
                var q = _context.DiseaseTypes.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    q = q.Where(x => x.Code.ToLower().Contains(keyword) || x.DiseaseName.ToLower().Contains(keyword));
                }

                var totalItems = await q.CountAsync(cancellationToken);
                var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

                var dbItems = await q.OrderBy(x => x.Code)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                var items = dbItems.Select(x => new CatalogItemDto
                {
                    Id = x.Code,
                    Group = "diseases",
                    Code = x.Code,
                    Name = x.DiseaseName,
                    Description = x.Description,
                    Status = "ACTIVE",
                    CreatedAt = null,
                    UpdatedAt = null
                }).ToList();

                return (items, totalItems, totalPages, page, pageSize);
            }
            else // allergies
            {
                var q = _context.AllergyTypes.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    if ((keyword.StartsWith("alg-") || keyword.StartsWith("alg")) && TryParseKeywordId(keyword, "alg-", "alg", out int id))
                    {
                        q = q.Where(x => x.AllergyId == id || x.AllergyName.ToLower().Contains(keyword));
                    }
                    else
                    {
                        q = q.Where(x => x.AllergyName.ToLower().Contains(keyword) || x.AllergyId.ToString().Contains(keyword));
                    }
                }

                var totalItems = await q.CountAsync(cancellationToken);
                var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

                var dbItems = await q.OrderBy(x => x.AllergyId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                var items = dbItems.Select(x => new CatalogItemDto
                {
                    Id = $"ALG{x.AllergyId:D3}",
                    Group = "allergies",
                    Code = $"ALG-{x.AllergyId:D3}",
                    Name = x.AllergyName,
                    Description = x.Severity,
                    Status = "ACTIVE",
                    CreatedAt = null,
                    UpdatedAt = null
                }).ToList();

                return (items, totalItems, totalPages, page, pageSize);
            }
        }

        public async Task<(bool Found, CatalogItemDto? Data)> GetItemByIdAsync(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id)) return (false, null);

            id = id.Trim();

            if (id.StartsWith("VAC", StringComparison.OrdinalIgnoreCase) && TryParseNumericSuffix(id, out var vacId))
            {
                var v = await _context.Vaccinations.AsNoTracking().FirstOrDefaultAsync(x => x.VaccinationId == vacId, cancellationToken);
                if (v == null) return (false, null);

                return (true, new CatalogItemDto
                {
                    Id = $"VAC{v.VaccinationId:D3}",
                    Group = "vaccines",
                    Code = $"VAC-{v.VaccinationId:D3}",
                    Name = v.Name,
                    Description = null,
                    Status = "ACTIVE",
                    CreatedAt = null,
                    UpdatedAt = null
                });
            }

            if (id.StartsWith("DIS", StringComparison.OrdinalIgnoreCase))
            {
                var d = await _context.DiseaseTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Code == id, cancellationToken);
                if (d == null) return (false, null);

                return (true, new CatalogItemDto
                {
                    Id = d.Code,
                    Group = "diseases",
                    Code = d.Code,
                    Name = d.DiseaseName,
                    Description = d.Description,
                    Status = "ACTIVE",
                    CreatedAt = null,
                    UpdatedAt = null
                });
            }

            if (id.StartsWith("ALG", StringComparison.OrdinalIgnoreCase) && TryParseNumericSuffix(id, out var allergyId))
            {
                var a = await _context.AllergyTypes.AsNoTracking().FirstOrDefaultAsync(x => x.AllergyId == allergyId, cancellationToken);
                if (a == null) return (false, null);

                return (true, new CatalogItemDto
                {
                    Id = $"ALG{a.AllergyId:D3}",
                    Group = "allergies",
                    Code = $"ALG-{a.AllergyId:D3}",
                    Name = a.AllergyName,
                    Description = a.Severity,
                    Status = "ACTIVE",
                    CreatedAt = null,
                    UpdatedAt = null
                });
            }

            return (false, null);
        }

        private static int NormalizePage(int? page) => page.GetValueOrDefault(1) <= 0 ? 1 : page!.Value;

        private static int NormalizePageSize(int? pageSize)
        {
            var value = pageSize.GetValueOrDefault(10);
            if (value <= 0) return 10;
            return Math.Clamp(value, 1, 100);
        }

        private static bool TryParseNumericSuffix(string id, out int value)
        {
            value = 0;
            if (id.Length < 4) return false;
            return int.TryParse(id[3..], out value);
        }

        private static bool TryParseKeywordId(string keyword, string prefix1, string prefix2, out int id)
        {
            id = 0;
            string numPart = keyword;
            if (keyword.StartsWith(prefix1))
                numPart = keyword[prefix1.Length..];
            else if (keyword.StartsWith(prefix2))
                numPart = keyword[prefix2.Length..];

            return int.TryParse(numPart, out id);
        }
    }
}
