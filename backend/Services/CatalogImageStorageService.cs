using ImageMagick;
using System.Text.RegularExpressions;

namespace Marketify.Services;

public sealed partial class CatalogImageStorageService(IWebHostEnvironment environment)
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private const long MaxDecodedPixels = 40_000_000;
    private const uint DerivativeQuality = 92;

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private static readonly ProductDerivativeSpec[] ProductDerivativeSpecs =
    [
        new(Path.Combine("responsive", "384"), 384, null),
        new("thumbnails", 640, 800),
        new(Path.Combine("responsive", "720"), 720, null),
        new("optimized", 1280, 1600)
    ];

    private readonly IWebHostEnvironment _environment = environment;

    public Task<StoredImageFile> SaveImageAsync(
        IFormFile file,
        string folder,
        string fileNamePrefix,
        CancellationToken cancellationToken = default)
    {
        return SaveImageCoreAsync(file, folder, fileNamePrefix, false, cancellationToken);
    }

    public Task<StoredImageFile> SaveProductImageAsync(
        IFormFile file,
        string fileNamePrefix,
        CancellationToken cancellationToken = default)
    {
        return SaveImageCoreAsync(file, "products", fileNamePrefix, true, cancellationToken);
    }

    public void DeleteIfExists(string? relativePath)
    {
        var absolutePath = ResolveAbsolutePath(relativePath);
        if (absolutePath is null)
        {
            return;
        }

        foreach (var path in GetProductImageFamilyPaths(absolutePath))
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    public string? ResolveAbsolutePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)
            || AbsoluteUriPattern().IsMatch(relativePath)
            || relativePath.StartsWith("//", StringComparison.Ordinal))
        {
            return null;
        }

        var normalized = relativePath.Replace('\\', '/').Trim().TrimStart('/');
        if (!normalized.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
        {
            normalized = $"images/{normalized}";
        }

        var webRootPath = Path.GetFullPath(GetWebRootPath());
        var absolutePath = Path.GetFullPath(Path.Combine(webRootPath, normalized.Replace('/', Path.DirectorySeparatorChar)));
        var webRootPrefix = webRootPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        return absolutePath.StartsWith(webRootPrefix, StringComparison.OrdinalIgnoreCase)
            ? absolutePath
            : null;
    }

    private async Task<StoredImageFile> SaveImageCoreAsync(
        IFormFile file,
        string folder,
        string fileNamePrefix,
        bool generateProductDerivatives,
        CancellationToken cancellationToken)
    {
        ValidateImageMetadata(file);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var stem = $"{SanitizePrefix(fileNamePrefix)}-{Guid.NewGuid():N}".TrimStart('-');
        var safeFileName = $"{stem}{extension}";
        var webRootPath = GetWebRootPath();
        var relativeDirectory = Path.Combine("images", folder).Replace('\\', '/');
        var absoluteDirectory = Path.Combine(webRootPath, "images", folder);
        var absolutePath = Path.Combine(absoluteDirectory, safeFileName);
        var relativePath = $"/{relativeDirectory}/{safeFileName}";
        var temporaryPaths = new List<string>();
        var publishedPaths = new List<string>();

        try
        {
            Directory.CreateDirectory(absoluteDirectory);
            var originalTemporaryPath = BuildTemporaryPath(absoluteDirectory, extension);
            temporaryPaths.Add(originalTemporaryPath);

            await using (var stream = new FileStream(originalTemporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var imageInfo = new MagickImageInfo(originalTemporaryPath);
            ValidateDecodedImage(imageInfo, extension);
            using var source = new MagickImage(originalTemporaryPath);

            var pendingDerivatives = generateProductDerivatives
                ? CreateProductDerivatives(source, absoluteDirectory, stem, temporaryPaths, cancellationToken)
                : [];

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(originalTemporaryPath, absolutePath);
            temporaryPaths.Remove(originalTemporaryPath);
            publishedPaths.Add(absolutePath);

            foreach (var derivative in pendingDerivatives)
            {
                File.Move(derivative.TemporaryPath, derivative.FinalPath);
                temporaryPaths.Remove(derivative.TemporaryPath);
                publishedPaths.Add(derivative.FinalPath);
            }

            return new StoredImageFile(relativePath, absolutePath);
        }
        catch (MagickException error)
        {
            DeleteFilesBestEffort(temporaryPaths.Concat(publishedPaths));
            throw new InvalidOperationException("The uploaded image could not be decoded or processed safely.", error);
        }
        catch
        {
            DeleteFilesBestEffort(temporaryPaths.Concat(publishedPaths));
            throw;
        }
    }

    private static List<PendingDerivative> CreateProductDerivatives(
        MagickImage source,
        string productDirectory,
        string stem,
        ICollection<string> temporaryPaths,
        CancellationToken cancellationToken)
    {
        source.AutoOrient();
        var derivatives = new List<PendingDerivative>();

        foreach (var spec in ProductDerivativeSpecs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var directory = Path.Combine(productDirectory, spec.RelativeDirectory);
            Directory.CreateDirectory(directory);
            var finalPath = Path.Combine(directory, $"{stem}.webp");
            var temporaryPath = BuildTemporaryPath(directory, ".webp");
            temporaryPaths.Add(temporaryPath);

            using var derivative = source.Clone();
            ResizeToFit(derivative, spec.MaxWidth, spec.MaxHeight);
            derivative.Strip();
            derivative.Format = MagickFormat.WebP;
            derivative.Quality = DerivativeQuality;
            derivative.Write(temporaryPath);

            derivatives.Add(new PendingDerivative(temporaryPath, finalPath));
        }

        return derivatives;
    }

    private static void ResizeToFit(IMagickImage<byte> image, uint maxWidth, uint? maxHeight)
    {
        if (image.Width <= maxWidth && (!maxHeight.HasValue || image.Height <= maxHeight.Value))
        {
            return;
        }

        image.Resize(new MagickGeometry(maxWidth, maxHeight ?? 0)
        {
            IgnoreAspectRatio = false
        });
    }

    private static void ValidateImageMetadata(IFormFile file)
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

    private static void ValidateDecodedImage(MagickImageInfo image, string extension)
    {
        var expectedFormats = extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => new[] { MagickFormat.Jpeg },
            ".png" => new[] { MagickFormat.Png },
            ".webp" => new[] { MagickFormat.WebP },
            _ => []
        };

        if (!expectedFormats.Contains(image.Format))
        {
            throw new InvalidOperationException("The uploaded file content does not match its image extension.");
        }

        if (image.Width == 0
            || image.Height == 0
            || image.Width > MaxDecodedPixels / image.Height)
        {
            throw new InvalidOperationException("The uploaded image dimensions are invalid or too large.");
        }
    }

    private IEnumerable<string> GetProductImageFamilyPaths(string absolutePath)
    {
        yield return absolutePath;

        var productDirectory = Path.Combine(GetWebRootPath(), "images", "products");
        var originalDirectory = Path.GetDirectoryName(absolutePath);
        if (!string.Equals(
                Path.GetFullPath(originalDirectory ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(productDirectory).TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        var stem = Path.GetFileNameWithoutExtension(absolutePath);
        foreach (var spec in ProductDerivativeSpecs)
        {
            yield return Path.Combine(productDirectory, spec.RelativeDirectory, $"{stem}.webp");
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

    private static string BuildTemporaryPath(string directory, string extension)
    {
        return Path.Combine(directory, $".upload-{Guid.NewGuid():N}.tmp{extension}");
    }

    private static void DeleteFilesBestEffort(IEnumerable<string> paths)
    {
        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Preserve the original upload/processing exception.
            }
        }
    }

    private static string SanitizePrefix(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return UnsafeFileNamePattern().Replace(normalized, "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex UnsafeFileNamePattern();

    [GeneratedRegex("^[a-z][a-z0-9+.-]*:", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AbsoluteUriPattern();

    private sealed record ProductDerivativeSpec(string RelativeDirectory, uint MaxWidth, uint? MaxHeight);
    private sealed record PendingDerivative(string TemporaryPath, string FinalPath);
}

public readonly record struct StoredImageFile(string RelativePath, string AbsolutePath);
