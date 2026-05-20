using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using EduHealth.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace EduHealth.Services.Implementations
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public async Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
        {
            if (file is null || file.Length == 0)
            {
                throw new ArgumentException("File is required.", nameof(file));
            }

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = string.IsNullOrWhiteSpace(folder) ? null : folder.Trim(),
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

            if (result.Error is not null)
            {
                throw new InvalidOperationException(result.Error.Message);
            }

            var url = result.SecureUrl?.ToString() ?? result.Url?.ToString();
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(result.PublicId))
            {
                throw new InvalidOperationException("Cloudinary upload failed.");
            }

            return (url, result.PublicId);
        }

        public async Task<(string Url, string PublicId)> UploadFileAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
        {
            if (file is null || file.Length == 0)
            {
                throw new ArgumentException("File is required.", nameof(file));
            }

            using var stream = file.OpenReadStream();

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = string.IsNullOrWhiteSpace(folder) ? null : folder.Trim(),
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var result = await _cloudinary.UploadAsync(uploadParams, null, cancellationToken);

            if (result.Error is not null)
            {
                throw new InvalidOperationException(result.Error.Message);
            }

            var url = result.SecureUrl?.ToString() ?? result.Url?.ToString();
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(result.PublicId))
            {
                throw new InvalidOperationException("Cloudinary upload failed.");
            }

            return (url, result.PublicId);
        }

        public async Task<bool> DeleteAsync(string publicId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(publicId)) return true;

            var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));

            if (result.Error is not null)
            {
                return false;
            }

            return string.Equals(result.Result, "ok", StringComparison.OrdinalIgnoreCase)
                || string.Equals(result.Result, "not found", StringComparison.OrdinalIgnoreCase);
        }
    }
}
