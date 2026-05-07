namespace EduHealth.DTOs.Messaging
{
    public class PagedResultDto<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
