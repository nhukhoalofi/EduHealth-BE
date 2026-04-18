using System.Text.Json;
using EduHealth.Data;
using EduHealth.DTOs.SystemLogs;
using EduHealth.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Services.Implementations
{
    public sealed class SystemLogService : ISystemLogService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly AppDbContext _context;

        public SystemLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(IReadOnlyList<SystemLogListItemDto> Items, int TotalItems, int TotalPages, int Page, int PageSize)> GetPagedAsync(SystemLogListQueryDto query, CancellationToken cancellationToken)
        {
            var page = NormalizePage(query.Page);
            var pageSize = NormalizePageSize(query.PageSize);

            var q = _context.SystemLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var kw = query.Keyword.Trim().ToLowerInvariant();
                q = q.Where(x => x.ActorName.ToLower().Contains(kw)
                                 || (x.ActorUsername != null && x.ActorUsername.ToLower().Contains(kw))
                                 || x.TargetLabel.ToLower().Contains(kw)
                                 || x.Description.ToLower().Contains(kw));
            }

            if (query.FromDate.HasValue)
            {
                var from = DateTime.SpecifyKind(query.FromDate.Value, DateTimeKind.Utc);
                q = q.Where(x => x.CreatedAt >= from);
            }

            if (query.ToDate.HasValue)
            {
                var to = DateTime.SpecifyKind(query.ToDate.Value, DateTimeKind.Utc);
                q = q.Where(x => x.CreatedAt <= to);
            }

            if (!string.IsNullOrWhiteSpace(query.Role))
            {
                var role = query.Role.Trim().ToUpperInvariant();
                q = q.Where(x => x.ActorRole == role);
            }

            if (!string.IsNullOrWhiteSpace(query.Module))
            {
                var module = query.Module.Trim().ToUpperInvariant();
                q = q.Where(x => x.Module == module);
            }

            if (!string.IsNullOrWhiteSpace(query.Action))
            {
                var action = query.Action.Trim().ToUpperInvariant();
                q = q.Where(x => x.Action == action);
            }

            // Stable pagination: order by CreatedAt desc then LogId desc
            q = q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.LogId);

            var totalItems = await q.CountAsync(cancellationToken);
            var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

            var items = await q.Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SystemLogListItemDto
                {
                    Id = x.LogId,
                    CreatedAt = x.CreatedAt,
                    ActorName = x.ActorName,
                    ActorUsername = x.ActorUsername,
                    ActorRole = x.ActorRole,
                    Module = x.Module,
                    Action = x.Action,
                    TargetType = x.TargetType,
                    TargetLabel = x.TargetLabel,
                    Description = x.Description,
                    Status = x.Status
                })
                .ToListAsync(cancellationToken);

            return (items, totalItems, totalPages, page, pageSize);
        }

        public async Task<(bool Found, SystemLogDetailDto? Data)> GetDetailAsync(long id, CancellationToken cancellationToken)
        {
            var log = await _context.SystemLogs.AsNoTracking().FirstOrDefaultAsync(x => x.LogId == id, cancellationToken);
            if (log == null) return (false, null);

            object? metadata = null;
            if (!string.IsNullOrWhiteSpace(log.MetadataJson))
            {
                try
                {
                    metadata = JsonSerializer.Deserialize<object>(log.MetadataJson, JsonOptions);
                }
                catch
                {
                    metadata = null;
                }
            }

            return (true, new SystemLogDetailDto
            {
                Id = log.LogId,
                CreatedAt = log.CreatedAt,
                ActorUserId = log.ActorUserId,
                ActorName = log.ActorName,
                ActorUsername = log.ActorUsername,
                ActorRole = log.ActorRole,
                Module = log.Module,
                Action = log.Action,
                TargetType = log.TargetType,
                TargetId = log.TargetId,
                TargetLabel = log.TargetLabel,
                Description = log.Description,
                Status = log.Status,
                Metadata = metadata
            });
        }

        private static int NormalizePage(int? page) => page.GetValueOrDefault(1) <= 0 ? 1 : page!.Value;

        private static int NormalizePageSize(int? pageSize)
        {
            var value = pageSize.GetValueOrDefault(10);
            if (value <= 0) return 10;
            return Math.Clamp(value, 1, 100);
        }
    }
}
