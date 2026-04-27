using EduHealth.DTOs.Catalogs;

namespace EduHealth.Services.Interfaces
{
    public interface ICatalogService
    {
        IReadOnlyList<CatalogGroupDto> GetGroups();
        Task<(IReadOnlyList<CatalogItemDto> Items, int TotalItems, int TotalPages, int Page, int PageSize)> GetItemsAsync(CatalogItemsQueryDto query, CancellationToken cancellationToken);
        Task<(bool Found, CatalogItemDto? Data)> GetItemByIdAsync(string id, CancellationToken cancellationToken);
    }
}
