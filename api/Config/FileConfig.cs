
namespace api.Config
{
    public class FileConfig
    {
        // === Limits ===
        public long MaxSizeBytes { get; set; } = 10 * 1024 * 1024; 
        public string[] AllowedContentTypes { get; set; } = new[] { "image/jpeg", "image/png", "image/webp" };
        public string[] AllowedExtensions { get; set; } = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        public int MaxFileNameLength { get; set; } = 150;

        // === Cloudinary ===
        public string CloudName { get; set; } = string.Empty;
        public string APIKey { get; set; } = string.Empty;
        public string APISrcset { get; set; } = string.Empty;
        public string DefaultFolder { get; set; } = "portfolio/uploads";

        public string CloudinaryUrl => $"cloudinary://{APIKey}:{APISrcset}@{CloudName}";

        public string? LocalUploadRoot { get; set; } = "wwwroot/uploads";

        public string? PublicBaseUrl { get; set; } = "https://localhost:4001";
        public string PublicBasePath { get; set; } = "/uploads";
    }
}
