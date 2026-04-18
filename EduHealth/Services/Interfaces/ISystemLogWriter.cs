namespace EduHealth.Services.Interfaces
{
    public interface ISystemLogWriter
    {
        Task WriteAsync(SystemLogWriteRequest request, CancellationToken cancellationToken);
    }

    public sealed class SystemLogWriteRequest
    {
        public DateTime? CreatedAt { get; set; }

        public int? ActorUserId { get; set; }
        public string? ActorName { get; set; }
        public string? ActorUsername { get; set; }
        public string? ActorRole { get; set; }

        public required string Module { get; set; }
        public required string Action { get; set; }

        public required string TargetType { get; set; }
        public string? TargetId { get; set; }
        public required string TargetLabel { get; set; }

        public required string Description { get; set; }
        public string Status { get; set; } = "SUCCESS";

        public object? Metadata { get; set; }
    }
}
