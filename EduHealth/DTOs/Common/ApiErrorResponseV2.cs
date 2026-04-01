namespace EduHealth.DTOs.Common
{
    public class ApiErrorResponseV2
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public IReadOnlyList<ApiErrorItemDto> Errors { get; set; } = Array.Empty<ApiErrorItemDto>();
        public DateTime Timestamp { get; set; }
        public string TraceId { get; set; } = null!;
    }
}
