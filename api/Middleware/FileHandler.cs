using api.Config;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

using Microsoft.Extensions.Options;

namespace api.Middleware
{
    public class FileHandler
    {
        private readonly FileConfig _cfg;
        private readonly Cloudinary _cloudinary;

        public FileHandler(IOptions<FileConfig> cfg, Cloudinary cloudinary)
        {
            _cfg = cfg.Value ?? throw new ArgumentNullException(nameof(cfg));
            _cloudinary = cloudinary ?? throw new ArgumentNullException(nameof(cloudinary));
        }


        /// <summary>
        /// Validates the incoming file using config limits.
        /// </summary>
        private void Validate(IFormFile file)
        {
            if (file is null || file.Length == 0)
                throw new ArgumentException("No file provided.", nameof(file));

            if (_cfg.MaxSizeBytes > 0 && file.Length > _cfg.MaxSizeBytes)
                throw new InvalidOperationException($"File exceeds max size of {_cfg.MaxSizeBytes} bytes.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (_cfg.AllowedExtensions?.Any() == true && !_cfg.AllowedExtensions.Contains(ext))
                throw new InvalidOperationException($"Extension '{ext}' is not allowed.");

            if (_cfg.AllowedContentTypes?.Any() == true && !_cfg.AllowedContentTypes.Contains(file.ContentType))
                throw new InvalidOperationException($"Content type '{file.ContentType}' is not allowed.");

            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            if (!string.IsNullOrWhiteSpace(baseName) && baseName.Length > _cfg.MaxFileNameLength)
                throw new InvalidOperationException($"File name is too long (max: {_cfg.MaxFileNameLength}).");
        }

        /// <summary>
        /// Uploads an image to Cloudinary and returns its secure CDN URL.
        /// </summary>
        public async Task<string> UploadPhoto(IFormFile file, CancellationToken ct = default)
        {
            Validate(file);

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only image uploads are allowed.");

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = _cfg.DefaultFolder,   // e.g., "portfolio/uploads"
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false,
                Transformation = new Transformation()
                    .Quality("auto")
                    .FetchFormat("auto")        // serve WebP/AVIF where supported
            };

            var result = await _cloudinary.UploadAsync(uploadParams, ct);

            if (result.StatusCode != System.Net.HttpStatusCode.OK)
                throw new InvalidOperationException($"Cloudinary upload failed: {result.Error?.Message}");

            // Consider persisting result.PublicId for deletes / derived transforms later
            return result.SecureUrl?.ToString()
                ?? throw new InvalidOperationException("No secure URL returned by Cloudinary.");
        }

        /// <summary>
        /// Optional: Upload any (allowed) file to local storage and return a public URL.
        /// Use this for non-image files or if you want local storage.
        /// </summary>
        public async Task<string> UploadFileToLocal(IFormFile file, CancellationToken ct = default)
        {
            Validate(file);

            if (string.IsNullOrWhiteSpace(_cfg.LocalUploadRoot))
                throw new InvalidOperationException("LocalUploadRoot is not configured.");

            Directory.CreateDirectory(_cfg.LocalUploadRoot);

            var ext = Path.GetExtension(file.FileName);
            var safeExt = string.IsNullOrWhiteSpace(ext) ? ".bin" : ext.ToLowerInvariant();
            var uniqueName = $"{Guid.NewGuid():N}{safeExt}";
            var fullPath = Path.Combine(_cfg.LocalUploadRoot!, uniqueName);

            await using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(fs);
            }

            var relative = $"{_cfg.PublicBasePath.TrimEnd('/')}/{uniqueName}";
            var url = string.IsNullOrWhiteSpace(_cfg.PublicBaseUrl)
                ? relative
                : $"{_cfg.PublicBaseUrl.TrimEnd('/')}{relative}";

            return url;
        }

        /// <summary>
        /// Build a transformed Cloudinary URL (thumbnail etc.) by PublicId without re-uploading.
        /// </summary>
        public string GetTransformedUrl(string publicId, int width, int height, string crop = "fill")
        {
            return _cloudinary.Api.UrlImgUp
                .Transform(new Transformation().Width(width).Height(height).Crop(crop).Quality("auto").FetchFormat("auto"))
                .BuildUrl(publicId);
        }

        /// <summary>
        /// Delete an image from Cloudinary by its PublicId.
        /// </summary>
        public async Task<bool> DeletePhoto(string publicId)
        {
            var res = await _cloudinary.DestroyAsync(new DeletionParams(publicId));
            return res.Result == "ok";
        }
    }
}

