namespace EduHealth.DTOs.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public T? Data { get; set; }
        public string? Field { get; set; }
        public object? Errors { get; set; }
        public object? Meta { get; set; }

        public static ApiResponse<T> Ok(T? data, string message) => new()
        {
            Success = true,
            Message = message,
            Data = data,
            Field = null,
            Errors = null,
            Meta = null
        };

        public static ApiResponse<T> Fail(string message, string? field = null, object? errors = null) => new()
        {
            Success = false,
            Message = message,
            Data = default,
            Field = field,
            Errors = errors,
            Meta = null
        };
    }
}
