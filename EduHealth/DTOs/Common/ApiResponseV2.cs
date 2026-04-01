namespace EduHealth.DTOs.Common
{
    public class ApiResponseV2<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public T? Data { get; set; }
        public object? Meta { get; set; }
        public DateTime Timestamp { get; set; }
        public string TraceId { get; set; } = null!;
    }
}
