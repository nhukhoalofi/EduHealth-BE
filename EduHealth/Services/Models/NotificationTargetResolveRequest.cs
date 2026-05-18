namespace EduHealth.Services.Models
{
    public sealed class NotificationTargetResolveRequest
    {
        public string? TargetMode { get; set; }
        public int? ClassId { get; set; }
        public IReadOnlyList<int>? RecipientUserIds { get; set; }
        public IReadOnlyList<string>? TargetRoles { get; set; }
    }
}
