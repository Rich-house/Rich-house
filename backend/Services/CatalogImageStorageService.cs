using System.Text.RegularExpressions;

namespace Marketify.Services;

public sealed partial class CatalogImageStorageService(IWebHostEnvironment environment)
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private readonly IWebHostEnvironment _environment = environment;

    public async Task<StoredImageFile> SaveImageAsync(
        IFormFile file,
        string folder,
        string fileNamePrefix,
        CancellationToken cancellationToken = default)
    {
        ValidateImage(file);

        var extension = Path.GetExtension(file.FileName);
        var safeFileName = $"{SanitizePrefix(fileNamePrefix)}-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var webRootPath = GetWebRootPath();
        var relativeDirectory = Path.Combine("images", folder).Replace('\\', '/');
        var absoluteDirectory = Path.Combine(webRootPath, "images", folder);

        Directory.CreateDirectory(absoluteDirectory);

        var absolutePath = Path.Combine(absoluteDirectory, safeFileName);
        var relativePath = $"/{relativeDirectory}/{safeFileName}";

        await using var stream = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, cancellationToken);

        return new StoredImageFile(relativePath, absolutePath);
    }

    public void DeleteIfExists(string? relativePath)
    {
        var absolutePath = ResolveAbsolutePath(relativePath);
        if (absolutePath is null || !File.Exists(absolutePath))
        {
            return;
        }

        File.Delete(absolutePath);
    }

    public string? ResolveAbsolutePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        if (Uri.TryCreate(relativePath, UriKind.Absolute, out _))
        {
            return null;
        }

        var normalized = relativePath.Replace('\\', '/').Trim();
        if (!normalized.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.TrimStart('/');
            normalized = $"images/{normalized}";
        }
        else
        {
            normalized = normalized.TrimStart('/');
        }

        return Path.Combine(GetWebRootPath(), normalized.Replace('/', Path.DirectorySeparatorChar));
    }

    private static void ValidateImage(IFormFile file)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("Please select a non-empty image file.");
        }

        if (file.Length > MaxImageBytes)
        {
            throw new InvalidOperationException("Images must be 5 MB or smaller.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedImageExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Only JPG, PNG, and WEBP images are supported.");
        }
    }

    private string GetWebRootPath()
    {
        if (!string.IsNullOrWhiteSpace(_environment.WebRootPath))
        {
            return _environment.WebRootPath;
        }

        var webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
        Directory.CreateDirectory(webRootPath);
        return webRootPath;
    }

    private static string SanitizePrefix(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return UnsafeFileNamePattern().Replace(normalized, "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex UnsafeFileNamePattern();
}

public readonly record struct StoredImageFile(string RelativePath, string AbsolutePath);
