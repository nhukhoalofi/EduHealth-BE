namespace EduHealth.DTOs.Catalogs
{
    public sealed class CatalogGroupDto
    {
        public required string Key { get; set; }
        public required string Label { get; set; }
    }

    public sealed class CatalogItemDto
    {
        public required string Id { get; set; }
        public required string Group { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required string Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public sealed class CatalogItemsQueryDto
    {
        public required string Group { get; set; }
        public string? Keyword { get; set; }
        public string? Status { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }
}
