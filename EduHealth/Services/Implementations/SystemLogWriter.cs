using System.Text.Json;
using EduHealth.Data;
using EduHealth.Data.Entities;
using EduHealth.Repositories.Interfaces;
using EduHealth.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Services.Implementations
{
    public sealed class SystemLogWriter : ISystemLogWriter
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly ISystemLogRepository _repository;
        private readonly AppDbContext _context;

        public SystemLogWriter(ISystemLogRepository repository, AppDbContext context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task WriteAsync(SystemLogWriteRequest request, CancellationToken cancellationToken)
        {
            string? actorName = string.IsNullOrWhiteSpace(request.ActorName) ? null : request.ActorName.Trim();
            string? actorRole = string.IsNullOrWhiteSpace(request.ActorRole) ? null : request.ActorRole.Trim().ToUpperInvariant();
            string? actorUsername = string.IsNullOrWhiteSpace(request.ActorUsername) ? null : request.ActorUsername.Trim();

            if (request.ActorUserId.HasValue && (actorName == null || actorRole == null))
            {
                var u = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == request.ActorUserId.Value, cancellationToken);
                if (u != null)
                {
                    actorName ??= u.FullName;
                    actorUsername ??= u.Username;
                    actorRole ??= u.Role;
                }
            }

            // Keep write-side resilient: do not throw on missing actor.
            actorName ??= "Hệ thống";
            actorRole ??= "SYSTEM";

            string? metadataJson = null;
            if (request.Metadata != null)
            {
                metadataJson = JsonSerializer.Serialize(request.Metadata, JsonOptions);
            }

            var log = new SystemLog
            {
                CreatedAt = request.CreatedAt ?? DateTime.UtcNow,
                ActorUserId = request.ActorUserId,
                ActorName = actorName,
                ActorUsername = actorUsername,
                ActorRole = actorRole,
                Module = request.Module.Trim().ToUpperInvariant(),
                Action = request.Action.Trim().ToUpperInvariant(),
                TargetType = request.TargetType.Trim(),
                TargetId = string.IsNullOrWhiteSpace(request.TargetId) ? null : request.TargetId.Trim(),
                TargetLabel = request.TargetLabel.Trim(),
                Description = request.Description.Trim(),
                Status = string.IsNullOrWhiteSpace(request.Status) ? "SUCCESS" : request.Status.Trim().ToUpperInvariant(),
                MetadataJson = metadataJson
            };

            await _repository.AddAsync(log, cancellationToken);
        }
    }
}
