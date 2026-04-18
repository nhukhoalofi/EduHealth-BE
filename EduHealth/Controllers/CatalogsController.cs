using EduHealth.DTOs.Catalogs;
using EduHealth.DTOs.Common;
using EduHealth.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHealth.Controllers
{
    [ApiController]
    [Route("api/v1/catalogs")]
    [Authorize(Roles = "ADMIN")]
    public sealed class CatalogsController : ControllerBase
    {
        private readonly ICatalogService _catalogService;

        public CatalogsController(ICatalogService catalogService)
        {
            _catalogService = catalogService;
        }

        [HttpGet("groups")]
        public IActionResult GetGroups()
        {
            var data = _catalogService.GetGroups();

            return Ok(new ApiResponseV2<IReadOnlyList<CatalogGroupDto>>
            {
                Success = true,
                Message = "Fetched successfully",
                Data = data,
                Meta = null,
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpGet("items")]
        public async Task<IActionResult> GetItems([FromQuery] CatalogItemsQueryDto query, CancellationToken cancellationToken)
        {
            var (items, totalItems, totalPages, page, pageSize) = await _catalogService.GetItemsAsync(query, cancellationToken);

            return Ok(new ApiResponseV2<IReadOnlyList<CatalogItemDto>>
            {
                Success = true,
                Message = "Fetched successfully",
                Data = items,
                Meta = new { page, pageSize, totalItems, totalPages },
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }

        [HttpGet("items/{id}")]
        public async Task<IActionResult> GetItemById([FromRoute] string id, CancellationToken cancellationToken)
        {
            var (found, data) = await _catalogService.GetItemByIdAsync(id, cancellationToken);

            if (!found)
            {
                return NotFound(new ApiErrorResponseV2
                {
                    Success = false,
                    Message = "Không tìm thấy danh mục.",
                    Errors = new[] { new ApiErrorItemDto { Field = "id", Code = "CATALOG_ITEM_NOT_FOUND", Message = "Không tồn tại item với id đã cung cấp." } },
                    Timestamp = DateTime.UtcNow,
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return Ok(new ApiResponseV2<CatalogItemDto>
            {
                Success = true,
                Message = "Fetched successfully",
                Data = data,
                Meta = null,
                Timestamp = DateTime.UtcNow,
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
