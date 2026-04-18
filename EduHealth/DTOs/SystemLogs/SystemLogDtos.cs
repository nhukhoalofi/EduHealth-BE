namespace EduHealth.DTOs.SystemLogs
{
    public sealed class SystemLogListItemDto
    {
        public long Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ActorName { get; set; } = null!;
        public string? ActorUsername { get; set; }
        public string ActorRole { get; set; } = null!;
        public string Module { get; set; } = null!;
        public string Action { get; set; } = null!;
        public string TargetType { get; set; } = null!;
        public string TargetLabel { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Status { get; set; } = null!;
    }

    public sealed class SystemLogDetailDto
    {
        public long Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ActorUserId { get; set; }
        public string ActorName { get; set; } = null!;
        public string? ActorUsername { get; set; }
        public string ActorRole { get; set; } = null!;
        public string Module { get; set; } = null!;
        public string Action { get; set; } = null!;
        public string TargetType { get; set; } = null!;
        public string? TargetId { get; set; }
        public string TargetLabel { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Status { get; set; } = null!;
        public object? Metadata { get; set; }
    }

    public sealed class SystemLogListQueryDto
    {
        public string? Keyword { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Role { get; set; }
        public string? Module { get; set; }
        public string? Action { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }
}
