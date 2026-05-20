using Microsoft.AspNetCore.Http;

namespace EduHealth.Services.Interfaces
{
    public interface ICloudinaryService
    {
        Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
        Task<(string Url, string PublicId)> UploadFileAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string publicId, CancellationToken cancellationToken = default);
    }
}
